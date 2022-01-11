using System;
using ContextFreeSession.Design;

namespace ConsoleTest
{
    using static GlobalTypeCombinator;

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("# Simple protocol");

            var simple = new GlobalType()
            {
                {"Start", Do("Sub"), Send("A", "C", "end") },
                {"Sub", Send("A", "C", "msg1"), Send("B", "A", "msg2"), Send("B", "A", Case("left", Send("A", "C", "msg3") ), Case("right")) },
            };

            Console.WriteLine("## Global type");
            Console.WriteLine(simple);

            Console.WriteLine("## Local type");
            Console.WriteLine(simple.ToLocal("A"));
            Console.WriteLine(simple.ToLocal("B"));
            Console.WriteLine(simple.ToLocal("C"));

            Console.WriteLine("## Local type (determinized)");
            Console.WriteLine(simple.Project("A"));
            Console.WriteLine(simple.Project("B"));
            Console.WriteLine(simple.Project("C"));

            Console.WriteLine("## Code");
            Console.WriteLine(simple.Generate());

            Console.WriteLine("# Tree protocol");

            var tree = new GlobalType()
            {
                { "Start", Do("Tree"), Send("Sender", "Logger", "end") },
                { "Tree", Send("Sender", "Receiver", Case("node", Do("Tree"), Do("Tree")),
                                                     Case<int>("leaf", Send<int>("Sender", "Logger", "leaf"))) }
            };

            Console.WriteLine("## Global type");
            Console.WriteLine(tree);

            Console.WriteLine("## Local type");
            Console.WriteLine(tree.ToLocal("Sender"));
            Console.WriteLine(tree.ToLocal("Receiver"));
            Console.WriteLine(tree.ToLocal("Logger"));

            Console.WriteLine("## Local type (determinized)");
            Console.WriteLine(tree.Project("Sender"));
            Console.WriteLine(tree.Project("Receiver"));
            Console.WriteLine(tree.Project("Logger"));

            Console.WriteLine("## Code");
            Console.WriteLine(tree.Generate());
        }
    }
}
