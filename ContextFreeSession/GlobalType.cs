using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeSession.Design
{
    public partial class GlobalType : IEnumerable<(string, GlobalTypeElement[])>
    {
        private readonly AssociationList<string, List<GlobalTypeElement>> rules = new();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<(string, GlobalTypeElement[])> GetEnumerator()
        {
            foreach (var (x, y) in rules)
            {
                yield return (x, y.ToArray());
            }
        }

        public void Add(string nonterminal, params GlobalTypeElement[] elements)
        {
            if (nonterminal is null)
            {
                throw new ArgumentNullException(nameof(nonterminal));
            }
            if (elements is null)
            {
                throw new ArgumentNullException(nameof(elements));
            }
            if (elements.Length < 1)
            {
                throw new ArgumentException("Attempted to add an empty rule.", nameof(elements));
            }
            if (!InvalidNonterminalSymbolException.IsValidNonterminalSymbol(nonterminal))
            {
                throw new InvalidNonterminalSymbolException(nonterminal);
            }
            if (rules.ContainsKey(nonterminal))
            {
                throw new IdentifierConflictException($"Nonterminal symbol '{nonterminal}' is already used.");
            }
            rules.Add(nonterminal, elements.ToList());
        }

        public override string ToString()
        {
            var accumlator = "";
            foreach (var (key, value) in rules)
            {
                accumlator += (key + " {").WithNewLine();
                foreach (var element in value)
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
                var res = new List<string>();
                foreach (var (key, value) in rules)
                {
                    res.AddRange(value.Where(x => x is Transfer).Cast<Transfer>().SelectMany(x => new string[] { x.From, x.To }));

                    res.AddRange(value.Where(x => x is Choice).Cast<Choice>().SelectMany(x => new string[] { x.From, x.To }));
                }
                return res.Distinct();
            }
        }


        public IEnumerable<string> Labels
        {
            get
            {
                var res = new List<string>();
                foreach (var (key, value) in rules)
                {
                    res.AddRange(value.Where(x => x is Transfer).Cast<Transfer>().Select(x => x.Label));

                    res.AddRange(value.Where(x => x is Choice).Cast<Choice>().SelectMany(x => x.Select(y => y.Item1)));
                }
                return res.Distinct();
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
        public string From { get; private set; }

        public string To { get; private set; }

        public string Label { get; private set; }

        public PayloadType PayloadType { get; private set; }

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

    public sealed class Choice : GlobalTypeElement, IEnumerable<(string, PayloadType, GlobalTypeElement[])>
    {
        public string From { get; private set; }

        public string To { get; private set; }

        private readonly List<(string label, PayloadType payloadType, List<GlobalTypeElement> conts)> Cases;

        public Choice(string from, string to, params (string label, PayloadType payloadType, GlobalTypeElement[] conts)[] cases)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            if (cases is null)
            {
                throw new ArgumentNullException(nameof(cases));
            }
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

        public IEnumerator<(string, PayloadType, GlobalTypeElement[])> GetEnumerator()
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
        public string Nonterminal { get; private set; }

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

        public static Choice Send(string from, string to, params (string, PayloadType, GlobalTypeElement[])[] cases) => new(from, to, cases);

        public static (string, PayloadType, GlobalTypeElement[]) Case(string label, params GlobalTypeElement[] conts) => (label, PayloadType.Create<Unit>(), conts);

        public static (string, PayloadType, GlobalTypeElement[]) Case<T>(string label, params GlobalTypeElement[] conts) => (label, PayloadType.Create<T>(), conts);

        public static Recursion Do(string nonterminal) => new(nonterminal);
    }
}
