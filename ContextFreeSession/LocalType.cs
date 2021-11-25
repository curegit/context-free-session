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

    public abstract class LocalTypeElement : LocalTypeTerm
    {
        internal LocalTypeElement() { }

        public abstract string ToTypeString();

        public virtual string ToExp() { return null; }
    }

    public sealed class Send : LocalTypeElement
    {
        public string To { get; private set; }

        public string Label { get; private set; }

        public Type PayloadType { get; private set; }

        public LocalTypeTerm Cont { get; private set; }

        internal Send(string to, string label, Type payloadType, LocalTypeTerm cont)
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
    }

    public sealed class Select : LocalTypeElement
    {
        public string To { get; private set; }

        public List<(string, Type, LocalTypeTerm)> Branches { get; private set; }

        internal Select(string to, IEnumerable<(string, Type payloadType, LocalTypeTerm)> branches)
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
    }

    public sealed class Receive : LocalTypeElement
    {
        public string From { get; private set; }

        public string Label { get; private set; }

        public Type PayloadType { get; private set; }

        public LocalTypeTerm Cont { get; private set; }

        internal Receive(string from, string label, Type payloadType, LocalTypeTerm cont)
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
