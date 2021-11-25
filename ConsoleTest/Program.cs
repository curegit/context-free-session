using System;
using System.Collections.Generic;

using ContextFreeSession.Design;

namespace ConsoleTest
{
    using static GlobalTypeCombinator;

    public class Program
    {
        public static void Main(string[] args)
        {
            var intArray = new List<int>() { 1, 2, 3, 4, 5 };
            foreach (var item in intArray)
            {
                intArray.Add(item);
                Console.WriteLine(item);
                break;
            }


            var global = new GlobalType()
            {
                { "A", Send("A", "B", "ping"), Do("B") },
                { "B", Send("A", "B", "ping"), Do("T") },
                { "T", Send("B", "A", "pong"), Do("U") },
                { "U", Send("A", "C", Case<int>("op1", Send("C", "B", "op1")),
                                      Case<string>("op2", Send("C", "B", "op2"))), Send("B", "A", "end") }
            };

            Console.WriteLine(global);

            var tree = new GlobalType()
            {
                { "Start", Do("Tree"), Send("Sender", "Logger", "end") },
                { "Tree", Send("Sender", "Receiver", Case("node", Do("Tree"), Do("Tree")),
                                                     Case<int>("leaf", Send<int>("Sender", "Logger", "leaf"))) }
            };

            Console.WriteLine(tree);

            /*
            var sender = tree.Project("Sender");
            Console.WriteLine(sender);

            var receiver = tree.Project("Receiver");
            Console.WriteLine(receiver);
            */

            var logger = tree.Project("Logger");
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
