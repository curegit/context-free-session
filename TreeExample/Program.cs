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
            var s = t.Receive<Sender, leaf>(out var integer);

            Console.WriteLine(integer);

            return s.Do(LoggerTreeDeleg2);
        }

        Eps LoggerTreeDeleg2(Logger_Tree_ t)
        {
            return t.Branch<Sender, leaf, end>(leaf => leaf.Do(LoggerTreeDeleg).Do(LoggerTreeDeleg2), end => end);
        }
    }
);


        var tree = new Node(new Leaf(1), new Node(new Leaf(2), new Leaf(3)));

        sender.Do(SendTree, tree).Send<Logger, end>(unit).Close();

        Eps SendTree(Sender_Tree ch, IntTree tree)
        {
            if (tree is Leaf leaf)
            {
                var n = leaf.Value;
                return ch.Send<Receiver, leaf>(n).Send<Logger, leaf>(n);
            }
            else
            {
                var (left, right) = ((Node)tree).Children;
                return ch.Send<Receiver, node>(unit).Do(SendTree, left).Do(SendTree, right);
            }
        }
    }
}

public class IntTree
{

}

public class Node : IntTree
{
    public IntTree Left { get; init; }

    public IntTree Right { get; init; }

    public (IntTree, IntTree) Children => (Left, Right);

    public Node(IntTree left, IntTree right)
    {
        Left = left;
        Right = right;
    }
}

public class Leaf : IntTree
{
    public int Value { get; init; }

    public Leaf(int value)
    {
        Value = value;
    }
}
