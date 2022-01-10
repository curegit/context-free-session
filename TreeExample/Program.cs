using ContextFreeSession;
using ContextFreeSession.Runtime;

public class Program
{
    public static void Main(string[] args)
    {

        var sender = Threading.Fork<SenderStart, ReceiverStart, LoggerStart>
(
    receiver =>
    {
        receiver.Do(ReceiverTreeDeleg).Close();

        Eps ReceiverTreeDeleg(ReceiverTree t)
        {
            return t.Branch<Sender, node, leaf>(node => node.Receive<Sender, node>(out var _).Do(ReceiverTreeDeleg).Do(ReceiverTreeDeleg),
                leaf => leaf.Receive<Sender, leaf>(out var integer)
            );
        }
    },
    logger =>
    {
        logger.Do(LoggerTreeDeleg).Receive<Sender, end>(out var _).Close();

        Eps LoggerTreeDeleg(LoggerTree t)
        {
            var s = t.Receive<Sender, leaf>(out var integer).Do(LoggerTreeDeleg2);

            Console.WriteLine(integer);

            return s;
        }

        Eps LoggerTreeDeleg2(LoggerTree_ t)
        {
            return t.Branch<Sender, leaf, end>(leaf => leaf.Do(LoggerTreeDeleg).Do(LoggerTreeDeleg2), end => end);
        }
    }
);

        sender.Do(x => x.Send<Receiver, leaf>(1).Send<Logger, leaf>(1)).Send<Logger, end>(new Unit()).Close();

    }
}

public struct Sender : IRole { }
public struct Logger : IRole { }
public struct Receiver : IRole { }
public struct end : ILabel { }
public struct node : ILabel { }
public struct leaf : ILabel { }
public class SenderStart : Call<SenderTree, SendSession<Logger, end, ContextFreeSession.Unit, Eps>> { public override string Role => "Sender"; }
public class SenderTree : SendSession<Receiver, node, ContextFreeSession.Unit, Call<SenderTree, Call<SenderTree, Eps>>, leaf, int, SendSession<Logger, leaf, int, Eps>> { public override string Role => "Sender"; }
public class LoggerStart : Call<LoggerTree, ReceiveSession<Sender, end, ContextFreeSession.Unit, Eps>> { public override string Role => "Logger"; }
public class LoggerTree : ReceiveSession<Sender, leaf, int, Call<LoggerTree_, Eps>> { public override string Role => "Logger"; }
public class LoggerTree_ : BranchSession<Sender, leaf, Call<LoggerTree, Call<LoggerTree_, Eps>>, end, Eps> { public override string Role => "Logger"; }
public class ReceiverStart : Call<ReceiverTree, Eps> { public override string Role => "Receiver"; }
public class ReceiverTree : BranchSession<Sender, node, ReceiveSession<Sender, node, ContextFreeSession.Unit, Call<ReceiverTree, Call<ReceiverTree, Eps>>>, leaf, ReceiveSession<Sender, leaf, int, Eps>> { public override string Role => "Receiver"; }
