using ContextFreeSession.Runtime;

public struct A : IRole { }

public struct B : IRole { }

public struct C : IRole { }

public struct msg1 : ILabel { }

public struct msg2 : ILabel { }

public struct late : ILabel { }

public struct combo : ILabel { }

public struct sub1 : ILabel { }

public struct sub2 : ILabel { }

public struct sub3 : ILabel { }

public struct tricky : ILabel { }

public struct super : ILabel { }

public struct uber : ILabel { }

public class A_Start : SendSession<B, msg1, ContextFreeSession.Unit, Call<A_Sub1, SendSession<C, late, ContextFreeSession.Unit, Eps>>, msg2, ContextFreeSession.Unit, Call<A_Sub1, SendSession<C, combo, ContextFreeSession.Unit, Eps>>>, IStart { public string Role => "A"; }

public class A_Sub1 : BranchSession<B, sub1, ReceiveSession<B, sub1, ContextFreeSession.Unit, SendSession<C, tricky, ContextFreeSession.Unit, Eps>>, sub2, ReceiveSession<B, sub2, ContextFreeSession.Unit, Call<A_Sub2, Eps>>, sub3, ReceiveSession<B, sub3, ContextFreeSession.Unit, Eps>>, IStart { public string Role => "A"; }

public class A_Sub2 : SendSession<C, super, string, Eps, uber, string, Eps>, IStart { public string Role => "A"; }

public class B_Start : BranchSession<A, msg1, ReceiveSession<A, msg1, ContextFreeSession.Unit, Call<B_Sub1, Eps>>, msg2, ReceiveSession<A, msg2, ContextFreeSession.Unit, Call<B_Sub1, Eps>>>, IStart { public string Role => "B"; }

public class B_Sub1 : SendSession<A, sub1, ContextFreeSession.Unit, Eps, sub2, ContextFreeSession.Unit, Call<B_Sub2, Eps>, sub3, ContextFreeSession.Unit, Eps>, IStart { public string Role => "B"; }

public class B_Sub2 : Eps, IStart { public string Role => "B"; }

public class C_Start : Call<C_Sub1, BranchSession<A, late, ReceiveSession<A, late, ContextFreeSession.Unit, Eps>, combo, ReceiveSession<A, combo, ContextFreeSession.Unit, Eps>>>, IStart { public string Role => "C"; }

public class C_Sub1 : BranchSession<A, tricky, ReceiveSession<A, tricky, ContextFreeSession.Unit, Eps>, (super, uber), Call<C_Sub2, Eps>, (late, combo), Eps>, IStart { public string Role => "C"; }

public class C_Sub2 : BranchSession<A, super, ReceiveSession<A, super, string, Eps>, uber, ReceiveSession<A, uber, string, Eps>>, IStart { public string Role => "C"; }
