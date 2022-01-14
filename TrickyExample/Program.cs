using System;
using ContextFreeSession.Runtime;
using static ContextFreeSession.Unit;

public class Program
{
    public static void Main(string[] args)
    {
        var a = Threading.Fork<A_Start, B_Start, C_Start>(b =>
        {
            b.Branch<A, msg1, msg2>(m1 =>
            {
                return m1.Receive<A, msg1>(out var _).Do(BSub1);
            },
            m2 =>
            {
                return m2.Receive<A, msg2>(out var _).Do(BSub1);
            }).Close();

            static Eps BSub1(B_Sub1 ch)
            {
                switch (Random.Shared.Next(3))
                {
                    case 0:
                        Console.WriteLine("B sends sub1.");
                        return ch.Send<A, sub1>(unit);
                    case 1:
                        Console.WriteLine("B sends sub2.");
                        return ch.Send<A, sub2>(unit);
                    default:
                        Console.WriteLine("B sends sub3.");
                        return ch.Send<A, sub3>(unit);
                }
            }
        },
        c =>
        {
            c.Do(CSub1).Branch<A, late, combo>(late => late.Receive<A, late>(out var _), combo => combo.Receive<A, combo>(out var _)).Close();

            static Eps CSub1(C_Sub1 ch)
            {
                return ch.Branch<A, tricky, (super, uber), (late, combo)>(
                    b1 => b1.Receive<A, tricky>(out var _),
                    b2 => b2.Do(CSub2),
                    b3 => b3);
            }

            static Eps CSub2(C_Sub2 ch)
            {
                return ch.Branch<A, super, uber>(super => super.Receive<A, super>(out var str1), uber => uber.Receive<A, uber>(out var str2));
            }
        });

        switch (Random.Shared.Next(2))
        {
            case 0:
                Console.WriteLine("A sends msg1.");
                a.Send<B, msg1>(unit).Do(ASub1).Send<C, late>(unit).Close();
                break;
            case 1:
                Console.WriteLine("A sends msg2.");
                a.Send<B, msg2>(unit).Do(ASub1).Send<C, combo>(unit).Close();
                break;
        }

        static Eps ASub1(A_Sub1 sub1)
        {
            return sub1.Branch<B, sub1, sub2, sub3>(
                b1 => b1.Receive<B, sub1>(out var _).Send<C, tricky>(unit),
                b2 => b2.Receive<B, sub2>(out var _).Do(ASub2),
                b3 => b3.Receive<B, sub3>(out var _));
        }

        static Eps ASub2(A_Sub2 sub2)
        {
            switch (Random.Shared.Next(2))
            {
                case 0:
                    Console.WriteLine("A sends super.");
                    return sub2.Send<C, super>("FS Misty 720");
                default:
                    Console.WriteLine("A sends uber.");
                    return sub2.Send<C, uber>("BS Rodeo 1080");
            }
        }
    }
}
