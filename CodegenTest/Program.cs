using ContextFreeSession.Runtime;

using ContextFreeSession;

var a = new LoggerStart();

a.Do(LS).recv<Sender, end>(out var n);

Eps LS(LoggerTree s)
{
    return s.recv<Sender, leaf>(out var b).Do(LS_);
}

Eps LS_(LoggerTree_ s)
{
    return s.branch(
        a => a.Do(LS).Do(LS_),
        b => b
        );
}

var b = new SenderStart();

b.Do(ST).send<Logger, end>();

Eps ST(SenderTree s)
{
    if (true)
    {
        return s.send<Receiver, node>(new ContextFreeSession.Unit()).Do(ST).Do(ST);
    }
    else
    {
        return s.send<Receiver, leaf>(3).send<Logger, leaf>(3);
    }
}



public struct Sender : IRole { }
public struct Logger : IRole { }
public struct Receiver : IRole { }
public struct end : ILabel { public string ToLabelString() { return "end"; } }
public struct node : ILabel { public string ToLabelString() { return "node"; } }
public struct leaf : ILabel { public string ToLabelString() { return "leaf"; } }
public class SenderStart : Call<SenderTree, Send<Logger, end, ContextFreeSession.Unit, Eps>> { }
public class SenderTree : Send<Receiver, node, ContextFreeSession.Unit, Call<SenderTree, Call<SenderTree, Eps>>, leaf, System.Int32, Send<Logger, leaf, System.Int32, Eps>> { }
public class LoggerStart : Call<LoggerTree, Receive<Sender, end, ContextFreeSession.Unit, Eps>> { }
public class LoggerTree : Receive<Sender, leaf, System.Int32, Call<LoggerTree_, Eps>> { }
public class LoggerTree_ : Branch<Sender, Labels<leaf>, Call<LoggerTree, Call<LoggerTree_, Eps>>, Labels<end>, Eps> { }
public class ReceiverStart : Call<ReceiverTree, Eps> { }
public class ReceiverTree : Branch<Sender, Labels<node>, Receive<Sender, node, ContextFreeSession.Unit, Call<ReceiverTree, Call<ReceiverTree, Eps>>>, Labels<leaf>, Receive<Sender, leaf, System.Int32, Eps>> { }
