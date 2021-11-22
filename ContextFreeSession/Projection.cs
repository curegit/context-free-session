using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextFreeSession
{
    public partial class GlobalType
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
                var newnont = role + t.Item1;
                dic.Add(newnont, LocalMap(role, t.Item2));
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
                            //return new RCBranch(t.From, t.Select(x => (x.Item1, x.Item2, LocalMap(role, x.Item3.Concat(elements.Skip(1))))));
                            return new Branch(t.From, t.Select(c => (c.Item1, new Receive(t.From, c.Item1, c.Item2, LocalMap(role, c.Item3.Concat(elements.Skip(1)))) as LocalTypeTerm)));
                        }
                        else
                        {
                            return new Merge(t.Select(x => LocalMap(role, x.Item3.Concat(elements.Skip(1)))));
                        }
                    case Recursion t:
                        var tail2 = LocalMap(role, elements.Skip(1));
                        return new Call(role + t.Nonterminal, tail2);
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
            SolveMerge();
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

        public void SolveMerge()
        {
            var changed = false;


            foreach (var rule in Rules.ToArray())
            {


                Rules[rule.Key] = SolveMergeSub(rule.Value, rule.Key);

                if (changed)
                {
                    SolveMerge();
                    return;
                }
            }


            LocalTypeTerm SolveMergeSub(LocalTypeTerm t, string context)
            {
                if (changed)
                {
                    return t;
                }
                switch (t)
                {
                    case Send s:
                        return new Send(s.To, s.Label, s.PayloadType, SolveMergeSub(s.Cont, context));
                    case Select sl:

                        var brs = sl.Branches.Select(x => (x.Item1, x.Item2, SolveMergeSub(x.Item3, context)));
                        return new Select(sl.To, brs);
                    case Receive r:
                        return new Receive(r.From, r.Label, r.PayloadType, SolveMergeSub(r.Cont, context));
                    //return SolveStarSub(r.Cont, context);
                    case Branch b:
                        var br = b.Branches.Select(x => (x.Item1, SolveMergeSub(x.Item2, context)));
                        return new Branch(b.From, br);
                    case Merge m:
                        var bss = m.Branches.Select(x => SolveMergeSub(x, context));
                        if (changed) return new Merge(bss);
                        changed = true;
                        return MergeStep(m, context);
                    //return new Merge(m.Branches.Select(x => SolveStarSub(x, context)));
                    case Star sc:
                        throw new NotImplementedException();
                    case Call c:
                        return new Call(c.Nonterminal, SolveMergeSub(c.Cont, context));
                    default:
                        return t;
                }

            }

            LocalTypeTerm MergeStep(Merge m, string context)
            {



                // 右側からmergeするので Merge はでてこない

                var brs = m.BranchesFlat.Cast<LocalTypeElement>();

                var t1 = brs.ElementAt(0);

                foreach (var b in brs.Skip(1))
                {
                    var t = Merge(t1, b);
                    if (t == null)
                    {
                        t1 = null;
                        break;
                    }
                    t1 = t;
                }
                if (t1 is not null)
                {
                    return t1;
                }



                //m.BranchesFlat.Select(x => (x, DirectorSet(x, context)));


                // m.UnderlayRecvs();
                //m.Others();
                // others に送信がある -> fail

                //var send = false;
                var from = new HashSet<string>();
                var recvs = new List<Receive>();
                var calls = new List<(string, Call)>();
                var callD = new Dictionary<string, string>();

                var eps = new List<(string, Epsilon)>();
                var epsD = new Dictionary<string, Epsilon>();


                foreach (var ele in brs)
                {
                    collect(ele);
                }

                if (from.Count != 1) throw new Exception();


                //var labs = recvs.GroupBy(x => x.Label).Select(x => (x.Key, new Receive(x.First().From, x.First().Label, x.First().PayloadType, new Merge(x.Select(z => z.Cont)).Simplify()))).Cast<(string, LocalTypeTerm)>();

                var labs = recvs.GroupBy(x => x.Label).Select(x => (x.Key, new Receive(x.First().From, x.First().Label, x.First().PayloadType, new Merge(x.Select(z => z.Cont)).Simplify()))).Select(x => (x.Key, x.Item2 as LocalTypeTerm));

                var fin = labs.ToList();
                fin.AddRange(calls.Select(x => (x.Item1, (LocalTypeTerm)x.Item2)));
                fin.AddRange(eps.Select(x => (x.Item1, (LocalTypeTerm)x.Item2)));

                return new Branch(from.First(), fin);



                void collect(LocalTypeElement t)
                {
                    switch (t)
                    {
                        case Branch branch:
                            foreach (var (l, b) in branch.Branches)
                            {
                                //if (b is Branch bb) collect(bb);
                                // !!!
                                from.Add(branch.From);
                                if (b is Call cc) { calls.AddRange(l.Select(x => (x, cc))); }
                                if (b is Epsilon e) { eps.AddRange(l.Select(x => (x, e))); }
                                if (b is LocalTypeElement ee) { collect(ee); }
                                else throw new Exception();
                            }
                            break;
                        case Receive receive:
                            from.Add(receive.From);
                            recvs.Add(receive);
                            break;
                        case Call call:
                            var res = DirectorSet(call, context);
                            if (res is null) throw new Exception();

                            from.Add(res.From);
                            /// !!!!
                            calls.AddRange(res.Labels.Select(x => (x, call)));
                            break;
                        case Epsilon epsilon:
                            var res2 = DirectorSet(epsilon, context);
                            if (res2 is null) throw new Exception();

                            from.Add(res2.From);
                            eps.AddRange(res2.Labels.Select(x => (x, epsilon)));
                            break;

                        default:
                            throw new Exception();
                    }
                }
            }







            LocalTypeElement? Merge(LocalTypeElement a, LocalTypeElement b)
            {
                return null;

                if (a == b)
                {
                    return a;
                }
                switch (a, b)
                {
                    case (Send s1, Send s2) when s1 == s2:
                        return new Send(s1.To, s1.Label, s1.PayloadType, new Merge(s1.Cont, s2.Cont));
                    case (Select l1, Select l2) when l1 == l2:
                        return new Select(l1.To, l1.Branches.Select(x => (x.Item1, x.Item2, (LocalTypeTerm)new Merge(x.Item3, l2.Branches.Where(y => y.Item1 == x.Item1).First().Item3))));
                    case (Call c1, Call c2) when c1.Nonterminal == c2.Nonterminal:
                        return new Call(c1.Nonterminal, new Merge(c1.Cont, c2.Cont));
                    default:
                        return null;
                }
            }
        }

        public void SolveStar()
        {
            var changed = false;


            foreach (var rule in Rules.ToArray())
            {


                Rules[rule.Key] = SolveStarSub(rule.Value, rule.Key);

                if (changed)
                {
                    SolveStar();
                    break;
                }
            }




            LocalTypeTerm SolveStarSub(LocalTypeTerm t, string context)
            {
                if (changed)
                {
                    return t;
                }


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

                        changed = true;
                        //StarStep(sc, context);
                        return StarStep(sc, context);
                    case Call c:
                        return new Call(c.Nonterminal, SolveStarSub(c.Cont, context));
                    default:
                        return t;
                }
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
            if (bs.IsEmpty) return new Epsilon();
            // bs が 1 以上 and recv なら

            // > fs が 0 失敗
            if (fs.IsEmpty) throw new Exception();
            // > fs が end 失敗
            // > fs が 1 以上 で recv のみ
            // >> 互いに素?
            if (!bs.Disjoint(fs))
            {
                throw new Exception();
            }
            // A -> alpha A'
            // A' -> beta A' | Eps
            var newSym = context + "_";
            var br1 = (bs.Labels.ToArray(), s.E.Append(new Call(newSym, new Epsilon())));
            var br2 = (fs.Labels.ToArray(), new Epsilon());
            var newRule = new Branch(bs.From, new List<(string[], LocalTypeTerm)>() { br1, br2 });
            Rules.Add(newSym, newRule);
            return new Call(newSym, new Epsilon());
        }

        private ReceiveCanditate? DirectorSet(LocalTypeTerm t, string context)
        {
            var f = FirstRecv(t);
            if (f is null)
            {
                return null;
            }
            else
            {
                if (f.Nullable)
                {
                    var fl = FollowRecv(context);
                    if (fl is null)
                    {
                        return null;
                    }
                    // ?
                    if (fl.Nullable)
                    {
                        return null;
                    }
                    return f.Union(fl);
                }
                return f;
            }
        }



        private ReceiveCanditate? FirstRecv(LocalTypeTerm t)
        {
            switch (t)
            {
                case Receive r:
                    return new ReceiveCanditate(r.From, false, new List<string>() { r.Label });
                case Branch b:
                    return new ReceiveCanditate(b.From, false, b.Branches.SelectMany(x => x.Item1));
                case Star:
                    // Union()
                    throw new NotImplementedException();
                case Merge m:
                    var vs = m.Branches.Select(FirstRecv);
                    return vs.Any(x => x is null) ? null : ReceiveCanditate.Union(vs);
                case Call c:
                    var cf = FirstRecv(Rules[c.Nonterminal]);
                    return cf is null ? null : (!cf.Nullable ? cf : cf.Union(FirstRecv(c.Cont)));
                //return cf is null ? null : (cf.Any() ? cf : ClousureFirstRecv(c.Cont));
                case Epsilon:
                    return ReceiveCanditate.Empty();
                case End:
                    throw new Exception();
                case Select:
                case Send:
                default:
                    return null;
            }


        }

        private ReceiveCanditate? FollowRecv(string nonterminal)
        {
            var l = new List<ReceiveCanditate>();
            foreach (var r in Rules.ToArray())
            {
                var f = FollowRecv2(nonterminal, r.Value, r.Key);
                if (f is null) return null;
                l.Add(f);
            }
            return ReceiveCanditate.Union(l);
        }

        private ReceiveCanditate? FollowRecv2(string nonterminal, LocalTypeTerm t, string context)
        {
            switch (t)
            {
                case Receive r:
                    return FollowRecv2(nonterminal, r.Cont, context);
                case Branch b:
                    var y = b.Branches.Select(x => FollowRecv2(nonterminal, x.Item2, context));
                    if (y.Any(x => x is null)) return null;
                    return ReceiveCanditate.Union(y);
                case Star st:
                    return FollowRecv2(nonterminal, st.E, context);
                case Merge m:
                    var ys = m.Branches.Select(x => FollowRecv2(nonterminal, x, context));
                    //var vs = m.Branches.Select(FirstRecv);
                    return ys.Any(x => x is null) ? null : ReceiveCanditate.Union(ys);
                case Call c:
                    //var cf = FirstRecv(Rules[c.Nonterminal]);
                    if (c.Nonterminal == nonterminal)
                    {
                        // 右再帰を無視
                        if (nonterminal == context && c.Cont is Epsilon)
                        {
                            return ReceiveCanditate.Empty();
                        }

                        var res = FollowRecvSub(nonterminal, c.Cont, context);
                        return res;
                    }

                    return FollowRecv2(nonterminal, c.Cont, context);
                case Epsilon:
                    return new ReceiveCanditate(null, false);
                case End:
                    return ReceiveCanditate.Empty();
                case Select sl:
                    var re = sl.Branches.Select(x => FollowRecv2(nonterminal, x.Item3, context));
                    return re.Any(x => x is null) ? null : ReceiveCanditate.Union(re);
                case Send s:
                    return FollowRecv2(nonterminal, s.Cont, context);
                default:
                    return null;
            }
        }

        private ReceiveCanditate? FollowRecvSub(string nonterminal, LocalTypeTerm t, string context)
        {
            var r = FirstRecv(t);
            if (r is null)
            {
                return null;
            }
            if (!r.Nullable)
            {
                return r;
            }
            else
            {
                var res = FollowRecv(context);
                if (res is null)
                {
                    return null;
                }
                var a = r.Union(res);
                if (a is null) return null;
                return new ReceiveCanditate(a.From, false, a.Labels);
            }
        }



        // star 内部の first
        // epsilon = []
        // null = fail
        private ReceiveCanditate? ClousureFirstRecv(LocalTypeTerm t)
        {
            switch (t)
            {
                case Receive r:
                    return new ReceiveCanditate(r.From, false, r.Label);
                case Branch b:
                    return new ReceiveCanditate(b.From, false, b.Branches.SelectMany(x => x.Item1));
                case Star:
                    throw new NotImplementedException();
                case Merge m:
                    var vs = m.Branches.Select(ClousureFirstRecv);
                    return vs.Any(x => x is null) ? null : ReceiveCanditate.Union(vs.Cast<ReceiveCanditate>());
                case Call c:
                    // c ga eps
                    var cf = FirstRecv(Rules[c.Nonterminal]);
                    //var cf = ClousureFirstRecv(Rules[c.Nonterminal]);
                    if (cf is null) return null;
                    if (cf.Nullable) return cf.Union(ClousureFirstRecv(c.Cont));
                    return cf;
                case Epsilon:
                    return ReceiveCanditate.Empty();
                case End:
                    throw new Exception();
                case Select:
                case Send:
                default:
                    return null;
            }
        }
    }

    /*
    internal class ReceiveCanditate
    {
        public string? From { get; init; }

        public IEnumerable<string> Labels { get; init; }

        public ReceiveCanditate(string? from, IEnumerable<string> labels)
        {
            From = from;
            Labels = labels.ToHashSet();
        }

        public ReceiveCanditate(string? from, params string[] labels)
        {
            From = from;
            Labels = labels.ToHashSet();
        }

        public ReceiveCanditate? Union(ReceiveCanditate canditate)
        {
            if (canditate.From == null)
            {
                return new ReceiveCanditate(From, Labels.Union(canditate.Labels));
            }
            else if (From == null || From == canditate.From)
            {
                return new ReceiveCanditate(canditate.From, Labels.Union(canditate.Labels));
            }
            else
            {
                return null;
            }
        }

        public static ReceiveCanditate Empty()
        {
            return new ReceiveCanditate(null, Enumerable.Empty<string>());
        }

        public static ReceiveCanditate? Union(IEnumerable<ReceiveCanditate> canditates)
        {
            ReceiveCanditate? accum = Empty();
            foreach (var c in canditates)
            {
                if (accum is null) return null;
                accum = accum.Union(c);
            }
            return accum;
        }
    }
    */

    internal class ReceiveCanditate
    {
        public string? From { get; init; }

        public bool Nullable { get; init; }

        public IEnumerable<string> Labels { get; init; }

        public ReceiveCanditate(string? from, bool nullable, IEnumerable<string> labels)
        {
            From = from;
            Nullable = nullable;
            Labels = labels.ToHashSet();
        }

        public ReceiveCanditate(string? from, bool nullable, params string[] labels)
        {
            From = from;
            Nullable = nullable;
            Labels = labels.ToHashSet();
        }

        public bool IsEmpty
        {
            get
            {
                return From == null;
            }
        }

        public bool Disjoint(ReceiveCanditate c)
        {
            return c.Labels.Except(Labels).Count() == c.Labels.Count();
        }

        public ReceiveCanditate? Union(ReceiveCanditate canditate)
        {
            if (canditate.From == null)
            {
                return new ReceiveCanditate(From, Nullable || canditate.Nullable, Labels.Union(canditate.Labels));
            }
            else if (From == null || From == canditate.From)
            {
                return new ReceiveCanditate(canditate.From, Nullable || canditate.Nullable, Labels.Union(canditate.Labels));
            }
            else
            {
                return null;
            }
        }

        public static ReceiveCanditate Empty()
        {
            return new ReceiveCanditate(null, true, Enumerable.Empty<string>());
        }

        public static ReceiveCanditate? Union(IEnumerable<ReceiveCanditate> canditates)
        {
            ReceiveCanditate? accum = new ReceiveCanditate(null, false);
            foreach (var c in canditates)
            {
                if (accum is null) return null;
                accum = accum.Union(c);
            }
            return accum;
        }
    }


}
