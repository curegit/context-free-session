using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeSession.Design
{
    public partial class GlobalType
    {
        public LocalType ToLocal(string role)
        {
            if (role is null) throw new ArgumentNullException(nameof(role));
            Validate(role);
            return new LocalType(role, MapToLocal(role, this));
        }

        public LocalType Project(string role)
        {
            if (role is null) throw new ArgumentNullException(nameof(role));
            var local = ToLocal(role);
            local.EliminateLeftRecursion();
            local.Determinize();
            local.Simplify();
            return local;
        }

        private void Validate(string role)
        {
            if (rules.Count == 0)
            {
                throw new InvalidGlobalTypeException("Rule set is empty.");
            }
            if (!Roles.Contains(role))
            {
                throw new ProjectionException($"Role '{role}' is not in this global type.");
            }
            foreach (var call in this.SelectMany(x => CollectCalls(x.body)).Distinct())
            {
                if (!this.Select(x => x.nonterminal).Contains(call)) throw new InvalidGlobalTypeException($"Calling undefined nonterminal '{call}'.");
            }
            foreach (var label in Labels)
            {
                if (Roles.Any(x => x == label)) throw new InvalidGlobalTypeException($"Label '{label}' conflicts with a role name.");
                if (this.Any(x => x.nonterminal == label)) throw new InvalidGlobalTypeException($"Label '{label}' conflicts with a nonterminal.");
            }
            foreach (var r in Roles)
            {
                if (this.Any(x => x.nonterminal == r)) throw new InvalidGlobalTypeException($"Role '{r}' conflicts with a nonterminal.");
            }

            static IEnumerable<string> CollectCalls(IEnumerable<GlobalTypeElement> ts)
            {
                var result = new List<string>();
                result.AddRange(ts.Where(x => x is Recursion).Cast<Recursion>().Select(x => x.Nonterminal));
                result.AddRange(ts.Where(x => x is Choice).Cast<Choice>().SelectMany(x => x.SelectMany(y => CollectCalls(y.conts))));
                return result;
            }
        }

        private static AssociationList<string, LocalTypeTerm> MapToLocal(string role, GlobalType globalType)
        {
            var rules = new AssociationList<string, LocalTypeTerm>();
            foreach (var (nonterminal, body) in globalType)
            {
                var newNonterminal = MapNonterminal(nonterminal, role);
                rules.Add(newNonterminal, MapRuleToLocal(role, body));
            }
            return rules;
        }

        private static LocalTypeTerm MapRuleToLocal(string role, IEnumerable<GlobalTypeElement> body)
        {
            if (body.Any())
            {
                switch (body.First())
                {
                    case Transfer t:
                        if (role == t.From)
                        {
                            return new Send(t.To, t.Label, t.PayloadType, MapRuleToLocal(role, body.Skip(1)));
                        }
                        else if (role == t.To)
                        {
                            return new Receive(t.From, t.Label, t.PayloadType, MapRuleToLocal(role, body.Skip(1)));
                        }
                        else
                        {
                            return MapRuleToLocal(role, body.Skip(1));
                        }
                    case Choice c:
                        if (role == c.From)
                        {
                            return new Select(c.To, c.Select(b => (b.label, b.payloadType, MapRuleToLocal(role, b.conts.Concat(body.Skip(1))))));
                        }
                        else if (role == c.To)
                        {
                            return new Branch(c.From, c.Select(b => (b.label, new Receive(c.From, b.label, b.payloadType, MapRuleToLocal(role, b.conts.Concat(body.Skip(1)))) as LocalTypeTerm)));
                        }
                        else
                        {
                            return new Merge(c.Select(x => MapRuleToLocal(role, x.conts.Concat(body.Skip(1)))));
                        }
                    case Recursion r:
                        return new Call(MapNonterminal(r.Nonterminal, role), MapRuleToLocal(role, body.Skip(1)));
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                return new Epsilon();
            }
        }

        private static string MapNonterminal(string nonterminal, string role)
        {
            return role + "_" + nonterminal;
        }
    }

    public partial class LocalType
    {
        private bool eliminated = false;

        private bool determinized = false;

        public void EliminateLeftRecursion()
        {
            // この左再帰除去アルゴリズムは文法に循環と空規則がないなら必ず成功する
            for (var i = 0; i < Rules.Count; i++)
            {
                // 間接左再帰を除去
                for (var j = 0; j < i; j++)
                {
                    var (n1, t1) = Rules.ElementAt(i);
                    var (n2, t2) = Rules.ElementAt(j);
                    if (t1 is Call c && c.Nonterminal == n2)
                    {
                        Rules[n1] = t2.Append(c.Cont);
                    }
                    else if (t1 is Merge m)
                    {
                        Rules[n1] = new Merge(m.FlattenedBranches.Select(x => (x is Call c && c.Nonterminal == n2) ? t2.Append(c.Cont) : x)).Simplify();
                    }
                    else if (t1 is Star)
                    {
                        throw new NotImplementedException();
                    }
                }
                // 直接左再帰を除去
                var (n, t) = Rules.ElementAt(i);
                if (t is Call call && call.Nonterminal == n)
                {
                    // ガードされていない選択肢しかないので失敗
                    throw new LeftRecursionException("Calling is not guarded.", this);
                }
                else if (t is Merge m)
                {
                    var bs = m.FlattenedBranches;
                    var a = bs.Where(b => b is Call c && c.Nonterminal == n);
                    var betas = bs.Except(a);
                    var alphas = a.Select(b => ((Call)b).Cont);
                    if (!a.Any())
                    {
                        continue;
                    }
                    if (!betas.Any())
                    {
                        // ガードされていない選択肢しかないので失敗
                        throw new LeftRecursionException("Can't eliminate left recursion 'cause there's no guarded choices.", this);
                    }
                    var head = new Merge(betas).Simplify();
                    var term = new Merge(alphas).Simplify();
                    var star = new Star(term);
                    var body = head.Append(star);
                    Rules[n] = body;
                }
            }
            eliminated = true;
        }

        public void Determinize()
        {
            if (!eliminated)
            {
                EliminateLeftRecursion();
            }
            SolveStar();
            SolveMerge();
            determinized = true;
        }

        public void Simplify()
        {
            if (!determinized)
            {
                throw new InvalidOperationException();
            }
            foreach (var (nonterminal, body) in Rules.ToArray())
            {
                if (body is Epsilon)
                {
                    SimplifySub(nonterminal);
                    Simplify();
                    break;
                }
            }

            void SimplifySub(string emptyNonterminal)
            {
                Rules.Remove(emptyNonterminal);
                foreach (var (nonterminal, body) in Rules.ToArray())
                {
                    Rules[nonterminal] = SimplifyTerm(body, emptyNonterminal);
                }
            }

            LocalTypeTerm SimplifyTerm(LocalTypeTerm t, string emptyNonterminal)
            {
                switch (t)
                {
                    case Send send:
                        return new Send(send.To, send.Label, send.PayloadType, SimplifyTerm(send.Cont, emptyNonterminal));
                    case Select select:
                        var bs = select.Branches.Select(x => (x.label, x.payloadType, SimplifyTerm(x.cont, emptyNonterminal)));
                        return new Select(select.To, bs);
                    case Receive receive:
                        return new Receive(receive.From, receive.Label, receive.PayloadType, SimplifyTerm(receive.Cont, emptyNonterminal));
                    case Branch branch:
                        var brs = branch.Branches.Select(x => (x.labels, SimplifyTerm(x.cont, emptyNonterminal)));
                        return new Branch(branch.From, brs);
                    case Merge merge:
                        return new Merge(merge.Branches.Select(x => SimplifyTerm(x, emptyNonterminal)));
                    case Star star:
                        throw new NotImplementedException();
                    case Call call:
                        if (call.Nonterminal == emptyNonterminal) return SimplifyTerm(call.Cont, emptyNonterminal);
                        return new Call(call.Nonterminal, SimplifyTerm(call.Cont, emptyNonterminal));
                    default:
                        return t;
                }
            }
        }

        // クリーネ閉包がなくなるまでマージする
        private void SolveStar()
        {
            var changed = false;
            foreach (var (nonterminal, body) in Rules.ToArray())
            {
                Rules[nonterminal] = SolveStarSub(body, nonterminal);
                if (changed)
                {
                    SolveStar();
                    break;
                }
            }

            LocalTypeTerm SolveStarSub(LocalTypeTerm t, string context)
            {
                // 不整合が起こるので同時に複数箇所変更しない
                if (changed)
                {
                    return t;
                }
                switch (t)
                {
                    case Send send:
                        return new Send(send.To, send.Label, send.PayloadType, SolveStarSub(send.Cont, context));
                    case Select select:
                        var bs = select.Branches.Select(x => (x.label, x.payloadType, SolveStarSub(x.cont, context)));
                        return new Select(select.To, bs);
                    case Receive receive:
                        return new Receive(receive.From, receive.Label, receive.PayloadType, SolveStarSub(receive.Cont, context));
                    case Branch branch:
                        var brs = branch.Branches.Select(x => (x.labels, SolveStarSub(x.cont, context)));
                        return new Branch(branch.From, brs);
                    case Merge merge:
                        return new Merge(merge.Branches.Select(x => SolveStarSub(x, context)));
                    case Star star:
                        changed = true;
                        return StarStep(star, context);
                    case Call call:
                        return new Call(call.Nonterminal, SolveStarSub(call.Cont, context));
                    default:
                        return t;
                }
            }

            LocalTypeElement StarStep(Star star, string context)
            {
                var repeat = FirstSet(star.Term);
                var follow = FollowSet(context);
                // 送信等を含むのでマージ不可
                if (repeat is null || follow is null) throw new ProjectionException("Can't merge with sending state.", this);
                // ループが空の場合
                if (repeat.IsEmpty) return new Epsilon();
                // Follow に終了を含む場合
                if (follow.Nullable) throw new ProjectionException("Can't merge with end state.", this);
                // 送信元が違う場合
                if (repeat.From! != follow.From!) throw new ProjectionException("Can't merge receiving states from different roles.", this);
                // ラベルが互いに素でないなら失敗
                if (!repeat.Disjoint(follow)) throw new ProjectionException("Labels are not disjoint.", this);
                // マージ可能
                var newSym = context;
                while (Rules.ContainsKey(newSym))
                {
                    newSym += "_";
                }
                var loop = (repeat.Labels.ToArray(), star.Term.Append(new Call(newSym, new Epsilon())));
                var exit = (follow.Labels.ToArray(), new Epsilon());
                var newRule = new Branch(repeat.From!, new List<(string[], LocalTypeTerm)>() { loop, exit });
                Rules.Add(newSym, newRule);
                return new Call(newSym, new Epsilon());
            }
        }

        // マージ演算子がなくなるまでマージする
        private void SolveMerge()
        {
            var changed = false;
            foreach (var (nonterminal, body) in Rules.ToArray())
            {
                Rules[nonterminal] = SolveMergeSub(body, nonterminal);
                if (changed)
                {
                    SolveMerge();
                    return;
                }
            }

            LocalTypeTerm SolveMergeSub(LocalTypeTerm t, string context)
            {
                // 不整合が起こるので同時に複数箇所変更しない
                if (changed)
                {
                    return t;
                }
                switch (t)
                {
                    case Send send:
                        return new Send(send.To, send.Label, send.PayloadType, SolveMergeSub(send.Cont, context));
                    case Select select:
                        var bs = select.Branches.Select(x => (x.label, x.payloadType, SolveMergeSub(x.cont, context)));
                        return new Select(select.To, bs);
                    case Receive receive:
                        return new Receive(receive.From, receive.Label, receive.PayloadType, SolveMergeSub(receive.Cont, context));
                    case Branch branch:
                        var brs = branch.Branches.Select(x => (x.labels, SolveMergeSub(x.cont, context)));
                        return new Branch(branch.From, brs);
                    case Merge merge:
                        var bss = merge.Branches.Select(x => SolveMergeSub(x, context));
                        if (changed) return new Merge(bss);
                        changed = true;
                        return MergeStep(merge, context);
                    case Star star:
                        throw new NotImplementedException();
                    case Call call:
                        return new Call(call.Nonterminal, SolveMergeSub(call.Cont, context));
                    default:
                        return t;
                }
            }

            LocalTypeTerm MergeStep(Merge m, string context)
            {
                // 右側からマージするので Merge は出現しない
                var bs = m.FlattenedBranches.Cast<LocalTypeElement>();
                var e = bs.ElementAt(0);
                foreach (var b in bs.Skip(1))
                {
                    // 送信等のマージ
                    var t = EasyMerge(e, b);
                    if (t == null)
                    {
                        e = null;
                        break;
                    }
                    e = t;
                }
                if (e is not null)
                {
                    return e;
                }
                // 以下は受信についてのマージ
                var nullable = false;
                var from = new HashSet<string>();
                var recvs = new List<Receive>();
                var calls = new List<(string nonterminal, Call call)>();
                var callLabels = new List<(string nonterminal, string label)>();
                var epsLabels = new List<string>();
                foreach (var b in bs)
                {
                    Collect(b);
                }
                // 終了のみの場合
                if (from.Count == 0)
                {
                    return new Epsilon();
                }
                // 終了と受信のマージ
                if (nullable)
                {
                    throw new ProjectionException("Can't merge with end state.", this);
                }
                // 異なるロールからの受信
                if (from.Count != 1)
                {
                    throw new ProjectionException("Can't merge receiving states from different roles.", this);
                }
                // ペイロードの型の一致を確認
                if (recvs.GroupBy(x => (x.Label, x.PayloadType)).Count() != recvs.GroupBy(x => x.Label).Count())
                {
                    throw new ProjectionException("Payload doesn't match.", this);
                }
                // ラベル集合が素集合であるか確認
                var recvLabelSet = new OrderedSet<string>(recvs.Select(x => x.Label));
                var callLabelSets = callLabels.GroupBy(x => x.nonterminal).Select(x => new OrderedSet<string>(x.Select(y => y.label)));
                var epsLabelSet = new OrderedSet<string>(epsLabels);
                var mustDisjoint = new List<OrderedSet<string>>();
                mustDisjoint.Add(recvLabelSet);
                mustDisjoint.AddRange(callLabelSets);
                mustDisjoint.Add(epsLabelSet);
                for (var i = 0; i < mustDisjoint.Count; i++)
                {
                    for (var j = i + 1; j < mustDisjoint.Count; j++)
                    {
                        if (!mustDisjoint[i].Disjoint(mustDisjoint[j]))
                        {
                            throw new ProjectionException("Labels are not disjoint.", this);
                        }
                    }
                }
                // マージ可能
                var brs = recvs.GroupBy(x => x.Label).Select(x => (new string[] { x.Key }, new Receive(x.First().From, x.Key, x.First().PayloadType, new Merge(x.Select(z => z.Cont)).Simplify()) as LocalTypeTerm)).ToList();
                if (calls.Any())
                {
                    var alist = new AssociationList<string, string[]>();
                    foreach (var (n, ls) in callLabels.GroupBy(x => x.nonterminal).Select(x => (x.Key, x.Select(y => y.label))))
                    {
                        alist.Add(n, ls.ToArray());
                    }
                    brs.AddRange(calls.GroupBy(x => x.nonterminal).Select(x => (alist[x.Key], new Call(x.Key, new Merge(x.Select(y => y.call.Cont)).Simplify()) as LocalTypeTerm)));
                }
                if (epsLabels.Any())
                {
                    brs.Add((epsLabels.ToArray(), new Epsilon()));
                }
                if (brs.Count > 1)
                {
                    return new Branch(from.First(), brs);
                }
                else
                {
                    var (ls, t) = brs.First();
                    return t;
                }

                // MPST 由来のマージ
                LocalTypeElement? EasyMerge(LocalTypeElement a, LocalTypeElement b)
                {
                    if (a == b)
                    {
                        return a;
                    }
                    switch (a, b)
                    {
                        case (Send s1, Send s2) when s1.To == s2.To && s1.Label == s2.Label && s1.PayloadType == s2.PayloadType:
                            return new Send(s1.To, s1.Label, s1.PayloadType, new Merge(s1.Cont, s2.Cont));
                        case (Select l1, Select l2) when l1.MergeConts(l2) is Select select:
                            return select;
                        case (Call c1, Call c2) when c1.Nonterminal == c2.Nonterminal:
                            return new Call(c1.Nonterminal, new Merge(c1.Cont, c2.Cont));
                        default:
                            return null;
                    }
                }

                void Collect(LocalTypeElement t)
                {
                    switch (t)
                    {
                        case Branch branch:
                            foreach (var (ls, b) in branch.Branches)
                            {
                                // 決定化済みの分岐は短絡評価する
                                from.Add(branch.From);
                                if (b is Call call)
                                {
                                    calls.Add((call.Nonterminal, call));
                                    callLabels.AddRange(ls.Select(x => (call.Nonterminal, x)));
                                }
                                if (b is Epsilon e)
                                {
                                    epsLabels.AddRange(ls);
                                }
                                if (b is LocalTypeElement s)
                                {
                                    Collect(s);
                                }
                                else throw new NotImplementedException();
                            }
                            break;
                        case Receive receive:
                            from.Add(receive.From);
                            recvs.Add(receive);
                            break;
                        case Call call:
                            var res1 = DirectorSet(call, context);
                            if (res1 is null) throw new ProjectionException("Can't merge with sending state.", this);
                            if (res1.Nullable) nullable = true;
                            if (res1.IsEmpty) return;
                            from.Add(res1.From!);
                            calls.Add((call.Nonterminal, call));
                            callLabels.AddRange(res1.Labels.Select(x => (call.Nonterminal, x)));
                            break;
                        case Epsilon epsilon:
                            var res2 = DirectorSet(epsilon, context);
                            if (res2 is null) throw new ProjectionException("Can't merge with sending state.", this);
                            if (res2.Nullable) nullable = true;
                            if (res2.IsEmpty) return;
                            from.Add(res2.From!);
                            epsLabels.AddRange(res2.Labels);
                            break;
                        default:
                            throw new ProjectionException("Can't merge with sending state.", this);
                    }
                }
            }
        }

        private ReceiveCanditate? FirstSet(LocalTypeTerm t)
        {
            switch (t)
            {
                case Receive receive:
                    return new ReceiveCanditate(receive.From, false, receive.Label);
                case Branch branch:
                    return new ReceiveCanditate(branch.From, false, branch.Branches.SelectMany(x => x.labels));
                case Star:
                    throw new NotImplementedException();
                case Merge merge:
                    var bs = merge.Branches.Select(FirstSet);
                    return bs.Any(x => x is null) ? null : ReceiveCanditate.Union(bs.Cast<ReceiveCanditate>());
                case Call call:
                    var cf = FirstSet(Rules[call.Nonterminal]);
                    if (cf is null) return null;
                    if (!cf.Nullable) return cf;
                    if (FirstSet(call.Cont) is ReceiveCanditate canditate) return cf.Union(canditate);
                    return null;
                case Epsilon:
                    return ReceiveCanditate.Empty();
                default:
                    return null;
            }
        }

        private ReceiveCanditate? FollowSet(string context)
        {
            var clist = new List<ReceiveCanditate>();
            foreach (var (nonterminal, body) in Rules.ToArray())
            {
                var follow = FollowSetRule(context, body, nonterminal);
                if (follow is null) return null;
                clist.Add(follow);
            }
            var union = ReceiveCanditate.Union(clist);
            if (union is null) return null;
            return context == StartSymbol ? new ReceiveCanditate(union.From, true, union.Labels) : union;
        }

        private ReceiveCanditate? FollowSetRule(string nonterminal, LocalTypeTerm t, string context)
        {
            switch (t)
            {
                case Send send:
                    return FollowSetRule(nonterminal, send.Cont, context);
                case Select select:
                    var brs = select.Branches.Select(x => FollowSetRule(nonterminal, x.cont, context));
                    return brs.Any(x => x is null) ? null : ReceiveCanditate.Union(brs.Cast<ReceiveCanditate>());
                case Receive receive:
                    return FollowSetRule(nonterminal, receive.Cont, context);
                case Branch branch:
                    var fs = branch.Branches.Select(x => FollowSetRule(nonterminal, x.cont, context));
                    if (fs.Any(x => x is null)) return null;
                    return ReceiveCanditate.Union(fs.Cast<ReceiveCanditate>());
                case Star star:
                    return FollowSetRule(nonterminal, star.Term, context);
                case Merge merge:
                    var bs = merge.Branches.Select(x => FollowSetRule(nonterminal, x, context));
                    return bs.Any(x => x is null) ? null : ReceiveCanditate.Union(bs.Cast<ReceiveCanditate>());
                case Call call:
                    if (call.Nonterminal == nonterminal)
                    {
                        // 右再帰を賢く無視
                        if (nonterminal == context && call.Cont is Epsilon)
                        {
                            return new ReceiveCanditate(null, false);
                        }
                        return FollowSetRuleSub(call.Cont, context);
                    }
                    return FollowSetRule(nonterminal, call.Cont, context);
                case Epsilon:
                    return new ReceiveCanditate(null, false);
                default:
                    return null;
            }
        }

        private ReceiveCanditate? FollowSetRuleSub(LocalTypeTerm t, string context)
        {
            var first = FirstSet(t);
            if (first is null) return null;
            if (!first.Nullable) return first;
            var follow = FollowSet(context);
            if (follow is null) return null;
            var union = first.Union(follow);
            if (union is null) return null;
            return new ReceiveCanditate(union.From, follow.Nullable, union.Labels);
        }

        private ReceiveCanditate? DirectorSet(LocalTypeTerm t, string context)
        {
            var first = FirstSet(t);
            if (first is null)
            {
                return null;
            }
            else
            {
                if (first.Nullable)
                {
                    var follow = FollowSet(context);
                    if (follow is null)
                    {
                        return null;
                    }
                    var union = first.Union(follow);
                    if (follow.Nullable)
                    {
                        return union;
                    }
                    return union is null ? null : new ReceiveCanditate(union.From, follow.Nullable, union.Labels);
                }
                return first;
            }
        }
    }

    internal class ReceiveCanditate
    {
        public string? From { get; init; }

        // ε または $ が含まれるかどうか
        public bool Nullable { get; init; }

        public OrderedSet<string> Labels { get; init; }

        public ReceiveCanditate(string? from, bool nullable, IEnumerable<string> labels)
        {
            From = from;
            Nullable = nullable;
            Labels = new OrderedSet<string>(labels);
        }

        public ReceiveCanditate(string? from, bool nullable, params string[] labels)
        {
            From = from;
            Nullable = nullable;
            Labels = new OrderedSet<string>(labels);
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
            ReceiveCanditate? accum = new(null, false);
            foreach (var c in canditates)
            {
                if (accum is null) return null;
                accum = accum.Union(c);
            }
            return accum;
        }
    }
}
