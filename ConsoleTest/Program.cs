using System;
using System.Numerics;
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
                { "Start", Do("Sub"), Send("A", "C", "end") },
                { "Sub", Send("A", "C", "msg1"), Send("B", "A", "msg2"), Send("B", "A", Case("left", Send("A", "C", "msg3") ), Case("right")) },
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
                                                     Case<int>("leaf", Send<int>("Sender", "Logger", "leaf"))) },
            };

            Console.WriteLine("## Global type");
            Console.WriteLine(tree);

            Console.WriteLine("## Local type");
            Console.WriteLine(tree.ToLocal("Sender"));
            Console.WriteLine(tree.ToLocal("Receiver"));
            var logger = tree.ToLocal("Logger");
            Console.WriteLine(logger);

            Console.WriteLine("### Left recursion elimination example");
            logger.EliminateLeftRecursion();
            Console.WriteLine(logger);

            Console.WriteLine("## Local type (determinized)");
            Console.WriteLine(tree.Project("Sender"));
            Console.WriteLine(tree.Project("Receiver"));
            Console.WriteLine(tree.Project("Logger"));

            Console.WriteLine("## Code");
            Console.WriteLine(tree.Generate());

            Console.WriteLine("# Merge sending example");

            var send = new GlobalType()
            {
                { "Start", Send("A", "B", Case("msg1", Send("C", "A", "const"), Send("A", "C", "msg1")),
                                          Case("msg2", Send("C", "A", "const"), Send("A", "C", "msg2"))), Send("C", "A", "bye") },
            };

            Console.WriteLine("## Global type");
            Console.WriteLine(send);

            Console.WriteLine("## Local type");
            Console.WriteLine(send.ToLocal("A"));
            Console.WriteLine(send.ToLocal("B"));
            Console.WriteLine(send.ToLocal("C"));

            Console.WriteLine("## Local type (determinized)");
            Console.WriteLine(send.Project("A"));
            Console.WriteLine(send.Project("B"));
            Console.WriteLine(send.Project("C"));

            Console.WriteLine("# Tricky example");

            var multiple = new GlobalType()
            {
                { "Start", Send("A", "B", Case("msg1", Do("Sub1"), Send("A", "C", "late")),
                                          Case("msg2", Do("Sub1"), Send("A", "C", "combo"))) },
                { "Sub1", Send("B", "A", Case("sub1", Send("A", "C", "tricky")),
                                         Case("sub2", Do("Sub2")),
                                         Case("sub3")) },
                { "Sub2", Send("A", "C", Case<string>("super"),
                                         Case<string>("uber")) },
            };

            Console.WriteLine("## Global type");
            Console.WriteLine(multiple);

            Console.WriteLine("## Local type");
            Console.WriteLine(multiple.ToLocal("A"));
            Console.WriteLine(multiple.ToLocal("B"));
            Console.WriteLine(multiple.ToLocal("C"));

            Console.WriteLine("## Local type (determinized)");
            Console.WriteLine(multiple.Project("A"));
            Console.WriteLine(multiple.Project("B"));
            Console.WriteLine(multiple.Project("C"));

            Console.WriteLine("## Code");
            Console.WriteLine(multiple.Generate());

            Console.WriteLine("# Bitcoin Miner");

            var btc = new GlobalType()
            {
                { "Start", Send("Client", "Miner", Case<(byte[] header, BigInteger target)>("mining", Send<(byte[] header, BigInteger hash)>("Miner", "Reporter", "found"), Send("Miner", "Client", "done"), Do("Start"), Send<(BigInteger hash, bool acceptance)>("Reporter", "Client", "result")),
                                                   Case("end", Send("Miner", "Reporter", "end"))) },
            };

            Console.WriteLine("## Global type");
            Console.WriteLine(btc);

            Console.WriteLine("## Local type");
            Console.WriteLine(btc.ToLocal("Client"));
            Console.WriteLine(btc.ToLocal("Miner"));
            Console.WriteLine(btc.ToLocal("Reporter"));

            Console.WriteLine("## Local type (determinized)");
            Console.WriteLine(btc.Project("Client"));
            Console.WriteLine(btc.Project("Miner"));
            Console.WriteLine(btc.Project("Reporter"));

            Console.WriteLine("## Code");
            Console.WriteLine(btc.Generate());
        }
    }
}
