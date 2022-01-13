using ContextFreeSession.Runtime;
using static ContextFreeSession.Unit;

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

        object tree = (1, (((2, 3), 4), (5, 6)));

        sender.Do(SendTree, tree).Send<Logger, end>(unit).Close();

        Eps SendTree(Sender_Tree ch, object tree)
        {
            if (tree is int n)
            {
                return ch.Send<Receiver, leaf>(n).Send<Logger, leaf>(n);
            }
            else
            {
                var (left, right) = ((object, object))tree;
                return ch.Send<Receiver, node>(unit).Do(SendTree, left).Do(SendTree, right);
            }
        }
    }
}
