using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ContextFreeSession.Runtime;
using static ContextFreeSession.Unit;

namespace BitcoinMiner
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var client = Threading.Fork<Client_Start, Miner_Start, Reporter_Start>(miner =>
            {

                MinerRecursion(miner).Close();

                Eps MinerRecursion(Miner_Start miner)
                {
                    return miner.Branch<Client, mining, end>(mining =>
                    {
                        var ch1 = mining.Receive<Client, mining>(out var info);
                        var (header, target) = info;

                        var ins = new NonceTester(header, target);

                        var (headerWithNonce, hash) = ins.Search();

                        var ch2 = ch1.Send<Reporter, found>((headerWithNonce, hash));

                        return ch2.Send<Client, done>(unit).Do(MinerRecursion);

                    },
                    end =>
                    {
                        return end.Receive<Client, end>(out var _).Send<Reporter, end>(unit);
                    });
                }
            },
            reporter =>
            {
                var hashList = new List<byte[]>();

                reporter.Branch<Miner, found, end>(reporter =>
                {



                },
                ch =>
                {

                    ch.Receive<Miner, end>();






                });

                Eps f(Miner_Start ch)
                {
                    var ch1 = ch.Receive<Miner, found>(out var pair);
                    var (header, hash) = pair;

                    hashList.Add(hash);

                    ch1.Do()

                    //

                hashList.First()




                    return;
                }


            });

            var blocks = Block.GetSampleBlocks().ToList();

            func(client).Close();

            Eps func(Client_Start ch)
            {
                if (blocks.Any())
                {
                    var block = blocks[0];
                    blocks.RemoveAt(0);

                    ch.Send<Miner, mining>((null, 1)).Receive();

                    Console.WriteLine();
                }
                else
                {
                    return ch.Send<Miner, end>(unit);
                }
            }
        }
    }
}
