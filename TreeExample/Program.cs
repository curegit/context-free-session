using ContextFreeSession;
using ContextFreeSession.Runtime;

public class Program
{
    public static void Main(string[] args)
    {

        var sender = Threading.Fork<Sender_Start, Receiver_Start, Logger_Start>
(
    receiver =>
    {
        receiver.Do(ReceiverTreeDeleg).Close();

        Eps ReceiverTreeDeleg(Receiver_Tree t)
        {
            return t.Branch<Sender, node, leaf>(node => node.Receive<Sender, node>(out var _).Do(ReceiverTreeDeleg).Do(ReceiverTreeDeleg),
                leaf => leaf.Receive<Sender, leaf>(out var integer)
            );
        }
    },
    logger =>
    {
        logger.Do(LoggerTreeDeleg).Receive<Sender, end>(out var _).Close();

        Eps LoggerTreeDeleg(Logger_Tree t)
        {
            var s = t.Receive<Sender, leaf>(out var integer).Do(LoggerTreeDeleg2);

            Console.WriteLine(integer);

            return s;
        }

        Eps LoggerTreeDeleg2(Logger_Tree_ t)
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

public class Sender_Start : Call<Sender_Tree, SendSession<Logger, end, ContextFreeSession.Unit, Eps>>, IStart { public string Role => "Sender"; }

public class Sender_Tree : SendSession<Receiver, node, ContextFreeSession.Unit, Call<Sender_Tree, Call<Sender_Tree, Eps>>, leaf, int, SendSession<Logger, leaf, int, Eps>>, IStart { public string Role => "Sender"; }

public class Logger_Start : Call<Logger_Tree, ReceiveSession<Sender, end, ContextFreeSession.Unit, Eps>>, IStart { public string Role => "Logger"; }

public class Logger_Tree : ReceiveSession<Sender, leaf, int, Call<Logger_Tree_, Eps>>, IStart { public string Role => "Logger"; }

public class Logger_Tree_ : BranchSession<Sender, leaf, Call<Logger_Tree, Call<Logger_Tree_, Eps>>, end, Eps>, IStart { public string Role => "Logger"; }

public class Receiver_Start : Call<Receiver_Tree, Eps>, IStart { public string Role => "Receiver"; }

public class Receiver_Tree : BranchSession<Sender, node, ReceiveSession<Sender, node, ContextFreeSession.Unit, Call<Receiver_Tree, Call<Receiver_Tree, Eps>>>, leaf, ReceiveSession<Sender, leaf, int, Eps>>, IStart { public string Role => "Receiver"; }
