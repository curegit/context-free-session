using System;
using ContextFreeSession.Design;

namespace ConsoleTest
{
    using static GlobalTypeCombinator;

    public class Program
    {
        public static void Main(string[] args)
        {
            var tree = new GlobalType()
            {
                { "Start", Do("Tree"), Send("Sender", "Logger", "end") },
                { "Tree", Send("Sender", "Receiver", Case("node", Do("Tree"), Do("Tree")),
                                                     Case<int>("leaf", Send<int>("Sender", "Logger", "leaf"))) }
            };

            Console.WriteLine("# Global type");
            Console.WriteLine(tree);

            

            var sender = tree.ToLocal("Sender");
            Console.WriteLine(sender);

            var receiver = tree.Project("Receiver");
            Console.WriteLine(receiver);


            var logger = tree.ToLocal("Logger");
            Console.WriteLine(logger);

            var s = tree.Generate();
            Console.WriteLine(s);

            var a = new GlobalType()
            {
                {"Start", Do("Sub"), Send("A", "C", "end") },
                {"Sub", Send("A", "C", "msg1"), Send("B", "A", "msg2"), Send("B", "A", Case("l", Send("A", "C", "s1") ), Case("r")) },
            };



            Console.WriteLine(a.Project("C"));



        }

    }
}
