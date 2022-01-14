using System;
using ContextFreeSession.Runtime;
using static ContextFreeSession.Unit;

public class Program
{
    public static void Main(string[] args)
    {
        var sender = Threading.Fork<Sender_Start, Receiver_Start, Logger_Start>(receiver =>
        {
            var (ch, tree) = receiver.DoFunc(ReceiveTree);
            Console.WriteLine(tree);
            ch.Close();

            (Eps, IntTree) ReceiveTree(Receiver_Tree ch)
            {
                return ch.BranchFunc<Sender, node, leaf, IntTree>(node =>
                {
                    var ch1 = node.Receive<Sender, node>(out var _);
                    var (ch2, left) = ch1.DoFunc(ReceiveTree);
                    var (ch3, right) = ch2.DoFunc(ReceiveTree);
                    return (ch3, new Node(left, right));
                },
                leaf =>
                {
                    var ch1 = leaf.Receive<Sender, leaf>(out var integer);
                    return (ch1, new Leaf(integer));
                });
            }
        },
        logger =>
        {
            logger.Do(LogLeaf).Receive<Sender, end>(out var _).Close();

            Eps LogLeaf(Logger_Tree ch)
            {
                var ch1 = ch.Receive<Sender, leaf>(out var integer);
                Console.WriteLine(integer);
                return ch1.Do(LogLeafBranch);
            }

            Eps LogLeafBranch(Logger_Tree_ ch)
            {
                return ch.Branch<Sender, leaf, end>(
                    leaf => leaf.Do(LogLeaf).Do(LogLeafBranch),
                    end => end);
            }
        });

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

public abstract class IntTree
{
    public override string ToString()
    {
        if (this is Node node)
        {
            return $"({node.Left} {node.Right})";
        }
        else
        {
            return ((Leaf)this).Value.ToString();
        }
    }
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
