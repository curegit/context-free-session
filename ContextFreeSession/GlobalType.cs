using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeSession.Design
{
    public partial class GlobalType : IEnumerable<(string nonterminal, GlobalTypeElement[] body)>
    {
        private readonly AssociationList<string, List<GlobalTypeElement>> rules = new();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<(string nonterminal, GlobalTypeElement[] body)> GetEnumerator()
        {
            foreach (var (nonterminal, body) in rules)
            {
                yield return (nonterminal, body.ToArray());
            }
        }

        public void Add(string nonterminal, params GlobalTypeElement[] body)
        {
            ArgumentNullException.ThrowIfNull(nonterminal);
            ArgumentNullException.ThrowIfNull(body);
            if (body.Length < 1)
            {
                throw new ArgumentException("Attempted to add an empty rule.", nameof(body));
            }
            if (!InvalidNonterminalSymbolException.IsValidNonterminalSymbol(nonterminal))
            {
                throw new InvalidNonterminalSymbolException(nonterminal);
            }
            if (rules.ContainsKey(nonterminal))
            {
                throw new IdentifierConflictException($"Nonterminal symbol '{nonterminal}' is already used.");
            }
            rules.Add(nonterminal, body.ToList());
        }

        public override string ToString()
        {
            var accumlator = "";
            foreach (var (nonterminal, body) in rules)
            {
                accumlator += (nonterminal + " {").WithNewLine();
                foreach (var element in body)
                {
                    accumlator += element.ToString().Indented(4).WithNewLine();
                }
                accumlator += "}".WithNewLine();
            }
            return accumlator.TrimNewLines();
        }

        public IEnumerable<string> Roles
        {
            get
            {
                var result = new List<string>();
                foreach (var (nonterminal, body) in rules)
                {
                    result.AddRange(Collect(body));
                }
                return result.Distinct();

                static IEnumerable<string> Collect(IEnumerable<GlobalTypeElement> ts)
                {
                    var result = new List<string>();
                    result.AddRange(ts.Where(x => x is Transfer).Cast<Transfer>().SelectMany(x => new string[] { x.From, x.To }));
                    result.AddRange(ts.Where(x => x is Choice).Cast<Choice>().SelectMany(x => (new string[] { x.From, x.To }).Concat(x.SelectMany(y => Collect(y.conts)))));
                    return result;
                }
            }
        }

        public IEnumerable<string> Labels
        {
            get
            {
                var result = new List<string>();
                foreach (var (nonterminal, body) in rules)
                {
                    result.AddRange(Collect(body));
                }
                return result.Distinct();

                static IEnumerable<string> Collect(IEnumerable<GlobalTypeElement> ts)
                {
                    var result = new List<string>();
                    result.AddRange(ts.Where(x => x is Transfer).Cast<Transfer>().Select(x => x.Label));
                    result.AddRange(ts.Where(x => x is Choice).Cast<Choice>().SelectMany(x => x.Select(y => y.label).Concat(x.SelectMany(y => Collect(y.conts)))));
                    return result;
                }
            }
        }
    }

    public abstract class GlobalTypeElement
    {
        internal GlobalTypeElement() { }

        public abstract override string ToString();
    }

    public sealed class Transfer : GlobalTypeElement
    {
        public readonly string From;

        public readonly string To;

        public readonly string Label;

        public readonly PayloadType PayloadType;

        public Transfer(string from, string to, string label, PayloadType payloadType)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            Label = label ?? throw new ArgumentNullException(nameof(label));
            PayloadType = payloadType ?? throw new ArgumentNullException(nameof(payloadType));
            if (!InvalidRoleNameException.IsValidRoleName(from))
            {
                throw new InvalidRoleNameException(from);
            }
            if (!InvalidRoleNameException.IsValidRoleName(to))
            {
                throw new InvalidRoleNameException(to);
            }
            if (from == to)
            {
                throw new ReflexiveMessageException(from);
            }
            if (!InvalidLabelException.IsValidLabel(label))
            {
                throw new InvalidLabelException(label);
            }
        }

        public override string ToString()
        {
            if (PayloadType.IsUnitType)
            {
                return $"{From} → {To}: {Label};";
            }
            else
            {
                return $"{From} → {To}: {Label}<{PayloadType}>;";
            }
        }
    }

    public sealed class Choice : GlobalTypeElement, IEnumerable<(string label, PayloadType payloadType, GlobalTypeElement[] conts)>
    {
        public readonly string From;

        public readonly string To;

        private readonly List<(string label, PayloadType payloadType, List<GlobalTypeElement> conts)> Cases;

        public Choice(string from, string to, params (string label, PayloadType payloadType, GlobalTypeElement[] conts)[] cases)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            ArgumentNullException.ThrowIfNull(cases);
            if (!InvalidRoleNameException.IsValidRoleName(from))
            {
                throw new InvalidRoleNameException(from);
            }
            if (!InvalidRoleNameException.IsValidRoleName(to))
            {
                throw new InvalidRoleNameException(to);
            }
            if (from == to)
            {
                throw new ReflexiveMessageException(from);
            }
            if (cases.Length < 2)
            {
                throw new InvalidGlobalTypeException($"Choice branches cannot be single.");
            }
            if (cases.Select(c => c.label).Distinct().Count() != cases.Length)
            {
                throw new InvalidGlobalTypeException($"Labels cannot be duplicated in choices.");
            }
            foreach (var (label, payloadType, conts) in cases)
            {
                if (!InvalidLabelException.IsValidLabel(label))
                {
                    throw new InvalidLabelException(label);
                }
            }
            Cases = cases.Select(c => (c.label, c.payloadType, c.conts.ToList())).ToList();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<(string label, PayloadType payloadType, GlobalTypeElement[] conts)> GetEnumerator()
        {
            return Cases.Select(c => (c.label, c.payloadType, c.conts.ToArray())).GetEnumerator();
        }

        public override string ToString()
        {
            var accumlator = $"{From} → {To}:";
            foreach (var (label, payloadType, conts) in Cases)
            {
                if (payloadType.IsUnitType)
                {
                    accumlator += $" {label} {{".WithNewLine();
                }
                else
                {
                    accumlator += $" {label}<{payloadType}> {{".WithNewLine();
                }
                foreach (var element in conts)
                {
                    accumlator += element.ToString().Indented(4).WithNewLine();
                }
                accumlator += "}";
            }
            return accumlator;
        }
    }

    public sealed class Recursion : GlobalTypeElement
    {
        public readonly string Nonterminal;

        public Recursion(string nonterminal)
        {
            Nonterminal = nonterminal ?? throw new ArgumentNullException(nameof(nonterminal));
        }

        public override string ToString()
        {
            return $"{Nonterminal}();";
        }
    }

    public static class GlobalTypeCombinator
    {
        public static Transfer Send(string from, string to, string label) => new(from, to, label, PayloadType.Create<Unit>());

        public static Transfer Send<T>(string from, string to, string label) => new(from, to, label, PayloadType.Create<T>());

        public static Choice Send(string from, string to, params (string label, PayloadType payloadType, GlobalTypeElement[] conts)[] cases) => new(from, to, cases);

        public static (string label, PayloadType payloadType, GlobalTypeElement[] conts) Case(string label, params GlobalTypeElement[] conts) => (label, PayloadType.Create<Unit>(), conts);

        public static (string label, PayloadType payloadType, GlobalTypeElement[] conts) Case<T>(string label, params GlobalTypeElement[] conts) => (label, PayloadType.Create<T>(), conts);

        public static Recursion Do(string nonterminal) => new(nonterminal);
    }
}
