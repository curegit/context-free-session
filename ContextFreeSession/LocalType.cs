using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeSession.Design
{
    public partial class LocalType : IEnumerable<(string nonterminal, LocalTypeTerm body)>
    {
        public readonly string Role;

        public string StartSymbol => Rules.ElementAt(0).key;

        private AssociationList<string, LocalTypeTerm> Rules { get; init; }

        internal LocalType(string role, AssociationList<string, LocalTypeTerm> rules)
        {
            Role = role;
            Rules = rules;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<(string nonterminal, LocalTypeTerm body)> GetEnumerator()
        {
            return Rules.GetEnumerator();
        }

        public override string ToString()
        {
            var accumulator = "";
            foreach (var (nonterminal, body) in Rules)
            {
                var str = body.ToString();
                accumulator += (nonterminal + " {").WithNewLine();
                accumulator += str == "" ? "" : str.Indented(4).WithNewLine();
                accumulator += "}".WithNewLine();
            }
            return accumulator.TrimNewLines();
        }
    }

    public abstract class LocalTypeTerm
    {
        internal LocalTypeTerm() { }

        public abstract LocalTypeTerm Append(LocalTypeTerm cont);

        public abstract override string ToString();
    }

    public sealed class Merge : LocalTypeTerm
    {
        public IEnumerable<LocalTypeTerm> Branches { get; private set; }

        internal Merge(IEnumerable<LocalTypeTerm> branches)
        {
            Branches = branches.ToList();
        }

        internal Merge(params LocalTypeTerm[] branches)
        {
            Branches = branches.ToList();
        }

        public IEnumerable<LocalTypeTerm> FlattenedBranches
        {
            get
            {
                return Branches.SelectMany(b => b is Merge merge ? merge.FlattenedBranches : new List<LocalTypeTerm>() { b }).ToList();
            }
        }

        public LocalTypeTerm Simplify()
        {
            if (!Branches.Any())
            {
                return new Epsilon();
            }
            if (Branches.Count() == 1)
            {
                return Branches.First();
            }
            return this;
        }

        public override LocalTypeTerm Append(LocalTypeTerm cont)
        {
            return new Merge(Branches.Select(b => b.Append(cont)));
        }

        public override string ToString()
        {
            var accumulator = "⊔";
            foreach (var (i, s) in Branches.Select((x, i) => (i, x.ToString())))
            {
                accumulator += (i == 0 ? "" : " |") + " {".WithNewLine() + (s == "" ? "" : s.Indented(4).WithNewLine()) + "}";
            }
            return accumulator;
        }
    }

    public sealed class Star : LocalTypeTerm
    {
        public LocalTypeTerm Term { get; private set; }

        internal Star(LocalTypeTerm term)
        {
            Term = term;
        }

        public override LocalTypeTerm Append(LocalTypeTerm cont)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"({Term})*;";
        }
    }

    public abstract class LocalTypeElement : LocalTypeTerm, IEquatable<LocalTypeElement>
    {
        internal LocalTypeElement() { }

        public abstract string ToTypeString();

        public abstract override int GetHashCode();

        public abstract override bool Equals(object? obj);

        public abstract bool Equals(LocalTypeElement? other);

        public static bool operator ==(LocalTypeElement? left, LocalTypeElement? right)
        {
            if (left is null)
            {
                if (right is null)
                {
                    return true;
                }
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(LocalTypeElement? lhs, LocalTypeElement? rhs) => !(lhs == rhs);
    }

    public sealed class Send : LocalTypeElement
    {
        public string To { get; private set; }

        public string Label { get; private set; }

        public PayloadType PayloadType { get; private set; }

        public LocalTypeTerm Cont { get; private set; }

        internal Send(string to, string label, PayloadType payloadType, LocalTypeTerm cont)
        {
            To = to;
            Label = label;
            PayloadType = payloadType;
            Cont = cont;
        }

        public override LocalTypeTerm Append(LocalTypeTerm cont)
        {
            return new Send(To, Label, PayloadType, Cont.Append(cont));
        }

        public override string ToString()
        {
            if (PayloadType.IsUnitType)
            {
                return ($"{To} ! {Label};".WithNewLine() + Cont.ToString()).TrimNewLines();
            }
            else
            {
                return ($"{To} ! {Label}<{PayloadType}>;".WithNewLine() + Cont.ToString()).TrimNewLines();
            }
        }

        public override string ToTypeString()
        {
            return $"SendSession<{To}, {Label}, {PayloadType.FullName}, {((LocalTypeElement)Cont).ToTypeString()}>";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(To, Label);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Send);
        }

        public override bool Equals(LocalTypeElement? other)
        {
            if (other is Send send)
            {
                return send.To == To && Label == send.Label && PayloadType == send.PayloadType && Cont == send.Cont;
            }
            else
            {
                return false;
            }
        }
    }

    public sealed class Select : LocalTypeElement
    {
        public readonly string To;

        public readonly IEnumerable<(string label, PayloadType payloadType, LocalTypeTerm cont)> Branches;

        internal Select(string to, IEnumerable<(string label, PayloadType payloadType, LocalTypeTerm cont)> branches)
        {
            To = to;
            Branches = branches.ToList();
        }

        public override LocalTypeTerm Append(LocalTypeTerm cont)
        {
            return new Select(To, Branches.Select(b => (b.label, b.payloadType, b.cont.Append(cont))));
        }

        public override string ToString()
        {
            var accumulator = $"{To} !";
            foreach (var (label, payloadType, cont) in Branches)
            {
                var str = cont.ToString();
                if (payloadType.IsUnitType)
                {
                    accumulator += $" {label} {{".WithNewLine() + (str == "" ? "" : str.Indented(4).WithNewLine()) + "}";
                }
                else
                {
                    accumulator += $" {label}<{payloadType}> {{".WithNewLine() + (str == "" ? "" : str.Indented(4).WithNewLine()) + "}";
                }
            }
            return accumulator;
        }

        public override string ToTypeString()
        {
            string str = string.Join(", ", Branches.SelectMany(x => new List<string>() { x.label, x.payloadType.FullName, ((LocalTypeElement)x.cont).ToTypeString() }));
            return $"SendSession<{To}, {str}>";
        }

        public override int GetHashCode()
        {
            return To.GetHashCode() + Branches.Count();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Select);
        }

        public override bool Equals(LocalTypeElement? other)
        {
            if (other is Select select)
            {
                if (select.To == To)
                {
                    var set1 = new HashSet<(string, PayloadType, LocalTypeTerm)>(Branches);
                    var set2 = new HashSet<(string, PayloadType, LocalTypeTerm)>(select.Branches);
                    return set1.SetEquals(set2);
                }
            }
            return false;
        }

        private bool Eq(Select other)
        {
            if (other.To == To)
            {
                var set1 = new HashSet<(string, PayloadType)>(Branches.Select(x => (x.label, x.payloadType)));
                var set2 = new HashSet<(string, PayloadType)>(other.Branches.Select(x => (x.label, x.payloadType)));
                return set1.SetEquals(set2);
            }
            return false;
        }

        public Select? MergeConts(Select other)
        {
            if (Eq(other))
            {
                var alist = new AssociationList<(string label, PayloadType payloadType), LocalTypeTerm>();
                foreach (var (label, payloadType, cont) in Branches)
                {
                    alist.Add((label, payloadType), cont);
                }
                foreach (var (label, payloadType, cont) in other.Branches)
                {
                    alist[(label, payloadType)] = new Merge(alist[(label, payloadType)], cont);
                }
                return new Select(To, alist.Select(x => (x.key.label, x.key.payloadType, x.value)));
            }
            return null;
        }
    }

    public sealed class Receive : LocalTypeElement
    {
        public readonly string From;

        public readonly string Label;

        public readonly PayloadType PayloadType;

        public readonly LocalTypeTerm Cont;

        internal Receive(string from, string label, PayloadType payloadType, LocalTypeTerm cont)
        {
            From = from;
            Label = label;
            PayloadType = payloadType;
            Cont = cont;
        }

        public override LocalTypeTerm Append(LocalTypeTerm cont)
        {
            return new Receive(From, Label, PayloadType, Cont.Append(cont));
        }

        public override string ToString()
        {
            if (PayloadType.IsUnitType)
            {
                return ($"{From} ? {Label};".WithNewLine() + Cont.ToString()).TrimNewLines();
            }
            else
            {
                return ($"{From} ? {Label}<{PayloadType}>;".WithNewLine() + Cont.ToString()).TrimNewLines();
            }
        }

        public override string ToTypeString()
        {
            return $"ReceiveSession<{From}, {Label}, {PayloadType.FullName}, {((LocalTypeElement)Cont).ToTypeString()}>";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(From, Label);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Receive);
        }

        public override bool Equals(LocalTypeElement? other)
        {
            if (other is Receive r)
            {
                return From == r.From && Label == r.Label && PayloadType == r.PayloadType && Cont == r.Cont;
            }
            return false;
        }
    }

    public class Branch : LocalTypeElement
    {
        public readonly string From;

        public readonly IEnumerable<(string[] labels, LocalTypeTerm cont)> Branches;

        internal Branch(string from, IEnumerable<(string label, LocalTypeTerm cont)> branches)
        {
            From = from;
            Branches = branches.Select(b => (new string[] { b.label }, b.cont)).ToList();
        }

        internal Branch(string from, IEnumerable<(string[] labels, LocalTypeTerm cont)> branches)
        {
            From = from;
            Branches = branches.ToList();
        }

        public override LocalTypeTerm Append(LocalTypeTerm cont)
        {
            return new Branch(From, Branches.Select(b => (b.labels, b.cont.Append(cont))));
        }

        public override string ToString()
        {
            var accumulator = $"{From} ¿";
            foreach (var (label, cont) in Branches)
            {
                var labelstr = string.Join(", ", label);
                var str = cont.ToString();
                accumulator += $" {labelstr} {{".WithNewLine() + (str == "" ? "" : str.Indented(4).WithNewLine()) + "}";
            }
            return accumulator;
        }

        public override string ToTypeString()
        {
            string str = string.Join(", ", Branches.SelectMany(x => new List<string>() { x.labels.Length > 1 ? "(" + string.Join(", ", x.labels) + ")" : x.labels[0], ((LocalTypeElement)x.cont).ToTypeString() }));
            return $"BranchSession<{From}, {str}>";
        }

        public override int GetHashCode()
        {
            return From.GetHashCode() + Branches.Count();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Branch);
        }

        public override bool Equals(LocalTypeElement? other)
        {
            if (other is Branch branch)
            {
                if (From == branch.From)
                {
                    var set1 = new HashSet<(OrderedSet<string>, LocalTypeTerm)>(Branches.Select(x => (new OrderedSet<string>(x.labels), x.cont)));
                    var set2 = new HashSet<(OrderedSet<string>, LocalTypeTerm)>(branch.Branches.Select(x => (new OrderedSet<string>(x.labels), x.cont)));
                    return set1.SetEquals(set2);
                }
            }
            return false;
        }
    }

    public class Call : LocalTypeElement
    {
        public readonly string Nonterminal;

        public readonly LocalTypeTerm Cont;

        internal Call(string nonterminal, LocalTypeTerm cont)
        {
            Nonterminal = nonterminal;
            Cont = cont;
        }

        public override LocalTypeTerm Append(LocalTypeTerm cont)
        {
            return new Call(Nonterminal, Cont.Append(cont));
        }

        public override string ToString()
        {
            return ($"{Nonterminal}();".WithNewLine() + Cont.ToString()).TrimNewLines();
        }

        public override string ToTypeString()
        {
            return $"Call<{Nonterminal}, {((LocalTypeElement)Cont).ToTypeString()}>";
        }

        public override int GetHashCode()
        {
            return Nonterminal.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Call);
        }

        public override bool Equals(LocalTypeElement? other)
        {
            if (other is Call c)
            {
                return Nonterminal == c.Nonterminal && Cont == c.Cont;
            }
            return false;
        }
    }

    public class Epsilon : LocalTypeElement
    {
        internal Epsilon() { }

        public override LocalTypeTerm Append(LocalTypeTerm cont)
        {
            return cont;
        }

        public override string ToString()
        {
            return "";
        }

        public override string ToTypeString()
        {
            return "Eps";
        }

        public override int GetHashCode()
        {
            return 1;
        }

        public override bool Equals(object? obj)
        {
            return obj is Epsilon;
        }

        public override bool Equals(LocalTypeElement? other)
        {
            if (other is Epsilon)
            {
                return true;
            }
            return false;
        }
    }
}
