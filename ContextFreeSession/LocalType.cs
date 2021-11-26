using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextFreeSession.Design
{
    public partial class LocalType
    {
        public readonly string Role;

        private readonly AssociationList<string, LocalTypeTerm> Rules;

        internal LocalType(string role, AssociationList<string, LocalTypeTerm> rs)
        {
            Role = role;
            Rules = rs;
        }

        public override string ToString()
        {
            var accumlator = "";
            foreach (var (nonterminal, value) in Rules)
            {
                accumlator += (nonterminal + " {").WithNewLine();

                accumlator += value.ToString().Indented(4).WithNewLine();

                accumlator += "}".WithNewLine();
            }
            return accumlator.TrimNewLines();
        }
    }

    public abstract class LocalTypeTerm
    {
        internal LocalTypeTerm() { }

        public abstract LocalTypeTerm Append(LocalTypeTerm local);

        public abstract override string ToString();
    }

    public sealed class Merge : LocalTypeTerm
    {
        public List<LocalTypeTerm> Branches { get; private set; }

        internal Merge(IEnumerable<LocalTypeTerm> branches)
        {
            Branches = branches.ToList();
            // Flatten merge?
            //Branches = branches.SelectMany(b => b is Merge merge ? merge.Branches : new List<LocalTypeTerm>() { b }).ToList();
        }

        internal Merge(params LocalTypeTerm[] localTypeTerms)
        {
            Branches = localTypeTerms.ToList();
        }

        public List<LocalTypeTerm> BranchesFlat
        {
            get
            {
                return Branches.SelectMany(b => b is Merge merge ? merge.BranchesFlat : new List<LocalTypeTerm>() { b }).ToList();
            }
        }

        public LocalTypeTerm Simplify()
        {
            if (Branches.Count() == 0)
            {
                return new Epsilon();
            }
            if (Branches.Count == 1)
            {
                return Branches.First();
            }
            return this;
        }

        public override string ToString()
        {
            var s = "Merge";

            foreach (var b in Branches)
            {
                s += " | {".WithNewLine() + b.ToString().TrimNewLines().Indented(4).WithNewLine() + "}";
            }
            return s;
        }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            return new Merge(Branches.Select(b => b.Append(local)));
        }
    }

    public sealed class Star : LocalTypeTerm
    {
        public LocalTypeTerm E { get; private set; }

        //public LocalTypeTerm Cont { get; private set; }

        internal Star(LocalTypeTerm e)
        {
            E = e;
        }

        public override string ToString()
        {
            return $"({E})*;".WithNewLine();
        }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class LocalTypeElement : LocalTypeTerm, IEquatable<LocalTypeElement>
    {
        internal LocalTypeElement() { }

        public abstract string ToTypeString();

        public abstract override int GetHashCode();

        public abstract override bool Equals(object? obj);

        public abstract bool Equals(LocalTypeElement? other);

        public virtual string ToExp() { return null; }

        public static bool operator ==(LocalTypeElement? left, LocalTypeElement? right)
        {
            if (left is null)
            {
                if (right is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
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

        public override string ToString()
        {
            return $"{To} ! {Label}<{PayloadType}>;".WithNewLine() + Cont.ToString();
        }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            return new Send(To, Label, PayloadType, Cont.Append(local));
        }

        public override string ToTypeString()
        {
            return $"Send<{To}, {Label}, {PayloadType.FullName}, {((LocalTypeElement)Cont).ToTypeString()}>";
        }

        public override string ToExp()
        {
            return $"new Send<{PayloadType}>({To}, {Label}, {((LocalTypeElement)Cont).ToExp()})";
        }

        public override int GetHashCode()
        {
            return (To, Label).GetHashCode();
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
        public string To { get; private set; }

        public List<(string, PayloadType, LocalTypeTerm)> Branches { get; private set; }

        internal Select(string to, IEnumerable<(string, PayloadType payloadType, LocalTypeTerm)> branches)
        {
            To = to;
            Branches = branches.ToList();
        }

        public override string ToString()
        {
            var s = $"{To} !";

            foreach (var b in Branches)
            {
                var (label, pay, c) = b;
                var bs = c.ToString().Indented(4).WithNewLine();
                s += $" {label}<{pay}> {{".WithNewLine() + bs + "}";
            }
            return s;
        }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            return new Select(To, Branches.Select(b => (b.Item1, b.Item2, b.Item3.Append(local))));
        }

        public override string ToTypeString()
        {
            string s = string.Join(" ,", Branches.SelectMany(x => new List<string>() { x.Item1, x.Item2.FullName, ((LocalTypeElement)x.Item3).ToTypeString() }));
            return $"Send<{To}, {s}>";
        }

        public override int GetHashCode()
        {
            return To.GetHashCode() + Branches.Count;
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

        public bool Eq(Select other)
        {
            if (other.To == To)
            {
                var set1 = new HashSet<(string, PayloadType)>(Branches.Select(x => (x.Item1, x.Item2)));
                var set2 = new HashSet<(string, PayloadType)>(other.Branches.Select(x => (x.Item1, x.Item2)));
                return set1.SetEquals(set2);
            }
            return false;
        }

        public Select? MergeCont(Select other)
        {
            if (Eq(other))
            {
                var alist = new AssociationList<(string, PayloadType), LocalTypeTerm>();
                foreach (var (l, t, c) in Branches)
                {
                    alist.Add((l, t), c);
                }
                foreach (var (l, t, c) in other.Branches)
                {
                    alist[(l, t)] = new Merge(alist[(l, t)], c);
                }
                return new Select(To, alist.Select(x => (x.Item1.Item1, x.Item1.Item2, x.Item2)));
            }
            return null;
        }
    }

    public sealed class Receive : LocalTypeElement
    {
        public string From { get; private set; }

        public string Label { get; private set; }

        public PayloadType PayloadType { get; private set; }

        public LocalTypeTerm Cont { get; private set; }

        internal Receive(string from, string label, PayloadType payloadType, LocalTypeTerm cont)
        {
            From = from;
            Label = label;
            PayloadType = payloadType;
            Cont = cont;
        }

        public override string ToString()
        {
            return $"{From} ? {Label}<{PayloadType}>;".WithNewLine() + Cont.ToString();
        }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            return new Receive(From, Label, PayloadType, Cont.Append(local));
        }

        public override string ToTypeString()
        {
            return $"Receive<{From}, {Label}, {PayloadType.FullName}, {((LocalTypeElement)Cont).ToTypeString()}>";
        }

        public override int GetHashCode()
        {
            return (From, Label).GetHashCode();
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
        public string From { get; private set; }

        public List<(string[], LocalTypeTerm)> Branches { get; private set; }

        internal Branch(string from, IEnumerable<(string, LocalTypeTerm)> branches)
        {
            From = from;
            Branches = branches.Select(b => (new string[] { b.Item1 }, b.Item2)).ToList();
        }

        internal Branch(string from, IEnumerable<(string[], LocalTypeTerm)> branches)
        {
            From = from;
            Branches = branches.ToList();
        }



        public override string ToString()
        {
            var s = $"{From} ??";

            foreach (var b in Branches)
            {
                var (label, c) = b;
                var labelstr = string.Join(",", label);
                var bs = c.ToString().Indented(4).WithNewLine();
                s += $" {labelstr} {{".WithNewLine() + bs + "}";
            }
            return s;
        }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            return new Branch(From, Branches.Select(b => (b.Item1, b.Item2.Append(local))));
        }

        public override string ToTypeString()
        {
            string s = string.Join(" ,", Branches.SelectMany(x => new List<string>() { "Labels<" + string.Join(" ,", x.Item1) + ">", ((LocalTypeElement)x.Item2).ToTypeString() }));
            return $"Branch<{From}, {s}>";
        }

        public override int GetHashCode()
        {
            return From.GetHashCode() + Branches.Count;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Branch);
        }


        public override bool Equals(LocalTypeElement? other)
        {
            if (other is Branch b)
            {
                // TODO
                return From == b.From;
            }
            return false;
        }
    }

    public class Call : LocalTypeElement
    {
        public string Nonterminal;

        public LocalTypeTerm Cont { get; private set; }

        internal Call(string c, LocalTypeTerm cont)
        {
            Nonterminal = c;
            Cont = cont;
        }

        public override string ToString()
        {
            return $"{Nonterminal}();".WithNewLine() + Cont.ToString();
        }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            return new Call(Nonterminal, Cont.Append(local));
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


        /*
        public string ToExp()
        {
            return $"new Call(\"{Nonterminal}\", {Cont.ToExp()})";
        }
        */
    }

    public class End : LocalTypeElement
    {
        internal End() { }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(LocalTypeElement? other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object? obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "END";
        }

        public override string ToTypeString()
        {
            throw new NotImplementedException();
        }
    }

    public class Epsilon : LocalTypeElement
    {
        internal Epsilon() { }

        public override LocalTypeTerm Append(LocalTypeTerm local)
        {
            return local;
        }

        public override bool Equals(LocalTypeElement? other)
        {
            if (other is Epsilon)
            {
                return true;
            }
            return false;
        }

        public override bool Equals(object? obj)
        {
            return obj is Epsilon;
        }

        public override int GetHashCode()
        {
            return 1;
        }

        public override string ToString()
        {
            return "";
        }

        public override string ToTypeString()
        {
            return "Eps";
        }
    }
}
