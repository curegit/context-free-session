using System;
using System.Collections.Generic;
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
                MinerStart(miner).Close();

                Eps MinerStart(Miner_Start ch)
                {
                    return ch.Branch<Client, mining, end>(mining =>
                    {
                        var ch1 = mining.Receive<Client, mining>(out var job);
                        var (header, target) = job;
                        var tester = new NonceTester(header, target);
                        var (headerWithNonce, hash) = tester.Search();
                        var ch2 = ch1.Send<Reporter, found>((headerWithNonce, hash));
                        return ch2.Send<Client, done>(unit).Do(MinerStart);
                    },
                    end =>
                    {
                        return end.Receive<Client, end>(out var _).Send<Reporter, end>(unit);
                    });
                }
            },
            reporter =>
            {
                var minedHashList = new Queue<BigInteger>();
                ReporterStart(reporter).Close();

                Eps ReporterStart(Reporter_Start ch)
                {
                    return ch.Branch<Miner, found, end>(found =>
                    {
                        var ch1 = found.Receive<Miner, found>(out var block);
                        var (header, hash) = block;
                        minedHashList.Enqueue(hash);

                        // (** ここでビットコインネットワークへ反映 **)

                        var ch2 = ch1.Do(ReporterStart);
                        var minedHash = minedHashList.Dequeue();
                        var acceptance = CheckMiningSuccess(minedHash);
                        return ch2.Send<Client, result>((minedHash, acceptance));
                    },
                    end =>
                    {
                        return end.Receive<Miner, end>(out var _);
                    });
                }

                bool CheckMiningSuccess(BigInteger hash)
                {
                    // (** ビットコインネットワークを見てマイニングの成否を返す **)
                    return true;
                }
            });

            var blocks = new Queue<Block>(Block.GetSampleBlocks());
            ClientStart(client).Close();

            Eps ClientStart(Client_Start ch)
            {
                if (blocks.Count != 0)
                {
                    var block = blocks.Dequeue();
                    var header = block.GetHeader();
                    var target = block.CalculateTarget();
                    var ch1 = ch.Send<Miner, mining>((header, target));
                    var ch2 = ch1.Receive<Miner, done>(out var _);
                    var ch3 = ch2.Do(ClientStart).Receive<Reporter, result>(out var outcome);
                    var (hash, acceptance) = outcome;
                    Console.WriteLine($"0x{hash:x}: {acceptance}");
                    return ch3;
                }
                else
                {
                    return ch.Send<Miner, end>(unit);
                }
            }
        }
    }
}
