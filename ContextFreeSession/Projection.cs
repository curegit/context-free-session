using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextFreeSession
{
    public partial class GlobalType : IEnumerable<(string, GlobalTypeElement[])>
    {
        public void Validate(string role)
        {
            //throw new NotImplementedException();
        }

        public LocalType Project(string role)
        {
            Validate(role);
            var ls = MapToLocalTerms(role, this);

            var local = new LocalType(role, ls);

            // local.SimplifyEps();
            // local.CheckLoop();

            local.EliminateLeftRecursion();

            Console.WriteLine(local);

            local.Determinize();

            return local;
        }

        private static SortedDictionary<string, LocalTypeTerm> MapToLocalTerms(string role, GlobalType globalType)
        {
            var dic = new SortedDictionary<string, LocalTypeTerm>();
            foreach (var t in globalType)
            {
                dic.Add(t.Item1, LocalMap(role, t.Item2));
            }
            return dic;
        }

        private static LocalTypeTerm LocalMap(string role, IEnumerable<GlobalTypeElement> elements)
        {
            if (elements.Any())
            {
                var head = elements.First();
                switch (head)
                {
                    case Transfer t:
                        var tail = LocalMap(role, elements.Skip(1));
                        if (role == t.From)
                        {
                            return new Send(t.To, t.Label, t.PayloadType, tail);
                        }
                        else if (role == t.To)
                        {
                            return new Receive(t.From, t.Label, t.PayloadType, tail);
                        }
                        else
                        {
                            return tail;
                        }
                    case Choice t:
                        if (role == t.From)
                        {
                            return new Select(t.To, t.Select(t => (t.Item1, t.Item2, LocalMap(role, t.Item3.Concat(elements.Skip(1))))));
                        }
                        else if (role == t.To)
                        {
                            return new Branch(t.From, t.Select(c => (c.Item1, new Receive(t.From, c.Item1, c.Item2, LocalMap(role, c.Item3.Concat(elements.Skip(1)))) as LocalTypeTerm)));
                        }
                        else
                        {
                            return new Merge(t.Select(x => LocalMap(role, x.Item3.Concat(elements.Skip(1)))));
                        }
                    case Recursion t:
                        var tail2 = LocalMap(role, elements.Skip(1));
                        return new Call(t.Nonterminal, tail2);
                    default:
                        throw new Exception();
                }
            }
            else
            {
                return new Epsilon();
            }
        }




    }

    public partial class LocalType
    {
        public void Determinize()
        {
            SolveStar();
        }

        // no loop, no epsilon
        public void EliminateLeftRecursion()
        {
            var rs = Rules;

            for (var i = 0; i < rs.Count; i++)
            {
                // kansetsu
                for (var j = 0; j < i; j++)
                {
                    var r = rs.ElementAt(i).Value;
                    var rkey = rs.ElementAt(i).Key;
                    var jsym = rs.ElementAt(j).Key;
                    var jr = rs.ElementAt(j).Value;
                    if (r is Call c && c.Nonterminal == jsym)
                    {
                        rs[rkey] = jr.Append(c.Cont);
                    }
                    else if (r is Merge m)
                    {
                        var bs = m.BranchesFlat.Select(x => (x is Call c && c.Nonterminal == jsym) ? jr.Append(c.Cont) : x);
                        rs[rkey] = new Merge(bs).Simplify();
                    }
                    else if (r is Star)
                    {
                        throw new NotImplementedException();
                    }
                }
                // direct
                var s = rs.ElementAt(i).Key;
                var e = rs.ElementAt(i).Value;
                if (e is Call && ((Call)e).Nonterminal == s)
                {
                    // 
                    throw new Exception();
                }
                else if (e is Merge)
                {
                    var m = (Merge)e;

                    var bs = m.BranchesFlat;

                    var als = bs.Where(b => b is Call && ((Call)b).Nonterminal == s);
                    var betas = bs.Except(als);

                    var alphas = als.Select(b => ((Call)b).Cont);

                    if (!als.Any())
                    {
                        continue;
                    }

                    if (!betas.Any())
                    {
                        throw new Exception();
                    }

                    var b1 = new Merge(betas).Simplify();
                    var ins = new Merge(alphas).Simplify();
                    var a1 = new Star(ins);
                    var app = b1.Append(a1);
                    rs[s] = app;
                }
            }
        }

        public void Merge1()
        {

        }

        public void MergeStep()
        {

        }

        public (string, LocalTypeTerm t)? Find(Func<LocalTypeTerm, bool> f)
        {
            foreach (var r in Rules)
            {
                var res = Find2(f, r.Value, r.Key);
                if (res is null) continue;
                return res;
            }
            return null;
        }

        public (string, LocalTypeTerm t)? Find2(Func<LocalTypeTerm, bool> f, LocalTypeTerm t, string context)
        {
            if (f(t))
            {
                return (context, t);
            }
            switch (t)
            {
                case Send s:
                    return Find2(f, s.Cont, context);
                case Select s:
                    foreach (var br in s.Branches)
                    {
                        var a = Find2(f, br.Item3, context);
                        if (a is null) continue;
                        return a;
                    }
                    return null;
                case Receive r:
                    return Find2(f, r.Cont, context);
                case Branch b:
                    foreach (var br in b.Branches)
                    {
                        var a = Find2(f, br.Item2, context);
                        if (a is null) continue;
                        return a;
                    }
                    return null;
                case Merge m:
                    foreach (var br in m.Branches)
                    {
                        var a = Find2(f, br, context);
                        if (a is null) continue;
                        return a;
                    }
                    return null;
                case Star sc:
                    return Find2(f, sc.E, context);
                case Call c:
                    return Find2(f, c.Cont, context);
                default:
                    return null;
            }
        }


        public void SolveStar()
        {
            foreach (var r in Rules.ToArray())
            {
                Rules[r.Key] = SolveStarSub(r.Value, r.Key);
            }
        }

        public LocalTypeTerm SolveStarSub(LocalTypeTerm t, string context)
        {
            switch (t)
            {
                case Send s:
                    return new Send(s.To, s.Label, s.PayloadType, SolveStarSub(s.Cont, context));
                case Select sl:
                    var brs = sl.Branches.Select(x => (x.Item1, x.Item2, SolveStarSub(x.Item3, context)));
                    return new Select(sl.To, brs);
                case Receive r:
                    return new Receive(r.From, r.Label, r.PayloadType, SolveStarSub(r.Cont, context));
                //return SolveStarSub(r.Cont, context);
                case Branch b:
                    var br = b.Branches.Select(x => (x.Item1, SolveStarSub(x.Item2, context)));
                    return new Branch(b.From, br);
                case Merge m:
                    return new Merge(m.Branches.Select(x => SolveStarSub(x, context)));
                case Star sc:
                    //StarStep(sc, context);
                    return StarStep(sc, context);
                case Call c:
                    return new Call(c.Nonterminal, SolveStarSub(c.Cont, context));
                default:
                    return t;
            }
        }






        private LocalTypeElement StarStep(Star s, string context)
        {
            Console.WriteLine(context);
            var bs = ClousureFirstRecv(s.E);
            var fs = FollowRecv(context);


            

            if (bs is null) throw new Exception();
            if (fs is null) throw new Exception();
            // bs が 0 (＝ε) なら 再帰なし
            if (!bs.Any()) return new Epsilon();
            // bs が 1 以上 and recv なら

            // > fs が 0 失敗
            if (!fs.Any()) throw new Exception();
            // > fs が end 失敗
            // > fs が 1 以上 で recv のみ
            // >> 互いに素?
            if (!(bs.Except(fs).Count() == bs.Count()))
            {
                throw new Exception();
            }
            // A -> alpha A'
            // A' -> beta A' | Eps
            var newSym = context + "_";
            var br1 = (bs.ToArray(), s.E.Append(new Call(newSym, new Epsilon())));
            var br2 = (fs.ToArray(), new Epsilon());
            var newRule = new Branch("", new List<(string[], LocalTypeTerm)>() { br1, br2 });
            Rules.Add(newSym, newRule);
            return new Call(newSym, new Epsilon());
        }

        private IEnumerable<string?>? FirstRecv(LocalTypeTerm t)
        {
            switch (t)
            {
                case Receive r:
                    return new List<string>() { r.Label };
                case Branch b:
                    return b.Branches.SelectMany(x => x.Item1);
                case Star:
                    throw new NotImplementedException();
                case Merge m:
                    var vs = m.Branches.Select(FirstRecv);
                    return vs.Any(x => x is null) ? null : vs.SelectMany(x => x);
                case Call c:
                    var cf = FirstRecv(Rules[c.Nonterminal]);
                    return cf is null ? null : (cf.Any() ? cf : ClousureFirstRecv(c.Cont));
                case Epsilon:
                    return new List<string?>() { null };
                case End:
                    throw new Exception();
                case Select:
                case Send:
                default:
                    return null;
            }


        }

        private IEnumerable<string>? FollowRecv(string nonterminal)
        {
            var l = new List<string>();
            foreach (var r in Rules.ToArray())
            {
                var f = FollowRecv2(nonterminal, r.Value, r.Key);
                if (f is null) return null;
                l.AddRange(f);
            }
            return l.Distinct();
        }

        private IEnumerable<string>? FollowRecv2(string nonterminal, LocalTypeTerm t, string context)
        {
            switch (t)
            {
                case Receive r:
                    return FollowRecv2(nonterminal, r.Cont, context);
                case Branch b:
                    var y = b.Branches.Select(x => FollowRecv2(nonterminal, x.Item2, context));
                    if (y.Any(x => x is null)) return null;
                    return y.SelectMany(x => x).Distinct();
                case Star st:
                    return FollowRecv2(nonterminal, st.E, context);
                case Merge m:
                    var ys = m.Branches.Select(x => FollowRecv2(nonterminal, x, context));
                    //var vs = m.Branches.Select(FirstRecv);
                    return ys.Any(x => x is null) ? null : ys.SelectMany(x => x).Distinct();
                case Call c:
                    //var cf = FirstRecv(Rules[c.Nonterminal]);
                    if (c.Nonterminal == nonterminal)
                    {
                        // 右再帰を無視
                        if (nonterminal == context && c.Cont is Epsilon)
                        {
                            return new List<string>() { };
                        }

                        var res = FollowRecvSub(nonterminal, c.Cont, context);
                        return res;
                    }

                    return FollowRecv2(nonterminal, c.Cont, context);
                case Epsilon:
                    return new List<string>() { };
                case End:
                    return new List<string>() { };
                case Select sl:
                    var re = sl.Branches.Select(x => FollowRecv2(nonterminal, x.Item3, context));
                    return re.Any(x => x is null) ? null : re.SelectMany(r => r).Distinct();
                case Send s:
                    return FollowRecv2(nonterminal, s.Cont, context);
                default:
                    return null;
            }
        }

        private IEnumerable<string>? FollowRecvSub(string nonterminal, LocalTypeTerm t, string context)
        {
            var r = FirstRecv(t);
            if (r is null)
            {
                return null;
            }
            if (r.All(x => x is not null))
            {
                return r;
            }
            else
            {
                return r.Where(x => x is not null).Union(FollowRecv(context));
            }
        }



        // star 内部の first
        // epsilon = []
        // null = fail
        private IEnumerable<string>? ClousureFirstRecv(LocalTypeTerm t)
        {
            switch (t)
            {
                case Receive r:
                    return new List<string>() { r.Label };
                case Branch b:
                    return b.Branches.SelectMany(x => x.Item1);
                case Star:
                    throw new NotImplementedException();
                case Merge m:
                    var vs = m.Branches.Select(ClousureFirstRecv);
                    return vs.Any(x => x is null) ? null : vs.SelectMany(x => x).Distinct();
                case Call c:
                    // c ga eps
                    var cf = FirstRecv(Rules[c.Nonterminal]);
                    //var cf = ClousureFirstRecv(Rules[c.Nonterminal]);
                    if (cf is null) return null;
                    if (cf.Any(x => x is null)) return cf.Where(x => x is not null).Union(ClousureFirstRecv(c.Cont)).Distinct();
                    return cf.Distinct();
                case Epsilon:
                    return Enumerable.Empty<string>();
                case End:
                    throw new Exception();
                case Select:
                case Send:
                default:
                    return null;
            }
        }

        /*
        // 受信のリスト、または空
        public List<(string role, string label)>? AllReceivePosibilities(SortedDictionary<string, LocalTypeTerm> rs, string current, LocalTypeTerm c)
        {
            switch (c)
            {
                case Receive r:
                    return new List<(string, string)> { (r.From, r.Label) };
                case Branch b:
                    return b.Branches.Select(t => (b.From, t.Item1)).ToList();
                case Merge m:
                    return m.Branches.SelectMany(x => AllReceivePosibilities(rs, current, x)).ToList();
                case Star e:
                    var a = AllReceivePosibilities(rs, current, e.E) is not null?.ToList();
                    var h = AllReceivePosibilities(rs, current, e.Cont).ToList();
                    return a.Union(h);
                case Call ca:
                    var all = AllReceivePosibilities(rs, ca.Nonterminal, rs[ca.Nonterminal]);
                    return all.Any() ? all : AllReceivePosibilities(rs, current, ca.Cont);
                case Epsilon eps:
                    return AllReceivePosibilities();
                case End:
                case Send s:
                case Select t:
                default:
                    throw new Exception();
            }
        }
        */

    }
}
