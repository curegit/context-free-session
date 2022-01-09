using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextFreeSession.Runtime
{
    public interface ILabel { }

    public interface IRole { }

    public class Labels<T>
    {

    }

    public class Session { }

    public class Send<To, L, T, S> : Session
    {
        public S send<D, label>() where D : To where label : L
        {
            return default(S);
        }

        public S send<D, label>(T a) where D : To where label : L
        {
            return default(S);
        }
    }

    public interface IUnitSend<To, L, S> { }

    public class Send1<To, L1, S1> : Session, IUnitSend<To, L1, S1>
    { }

    public class Send1<To, L1, S1, T1> : Session { }

    public class Send2<To, L1, S1, T1, L2, S2> : Send1<To, L1, S1, T1>, IUnitSend<To, L2, S2> { }

    public class Send2<To, L1, S1, L2, S2> : Send1<To, L1, S1>, IUnitSend<To, L2, S2>
    {
        S2 send<D, label2>() where D : To where label2 : L2 { return default(S2); }
    }

    public class Send<To, L1, T1, S1, L2, T2, S2> : Session, IUnitSend<To, L1, S1>
    {
        /*
        public S1 send<D, label1>(T1 t = default) where D : To where label1 : struct, L1 where D : struct, Unit
        {
            return default(S1);
        }

        public S2 send<D, label2>() where D : To where label2 : struct, L2
        {
            return default(S2);
        }
        */

        public S1 send<D, label1>(T1 t) where D : To where label1 : L1
        {
            return default(S1);
        }

        public S2 send<D, label2>(T2 a) where D : To where label2 : L2
        {
            return default(S2);
        }
    }

    public class Send<To, L1, S1, L2, T2, S2> : Send<To, L1, Unit, S1, T2, L2, S2>
    {
        public S1 send<D, La1>(Unit unit = default) where La1 : L1 { return default(S1); }

        public S2 send<D, La2>() where La2 : L2 { return default(S2); }
    }



    public static class SendEx
    {

    }

    public class Receive<From, L, T, S> : Session
    {
        public S recv<F, La>(out T a) where F : From where La : L
        {
            a = default(T);
            return default (S);
        }
    }

    public class Branch<From, LS1, S1, LS2, S2> : Session
    {
        public Eps branch(Func<S1, Eps> f1, Func<S2, Eps> f2)
        {
            return f1(default(S1));
        }
    }

    public class Call<S, SC> : Session
    {
        public SC Do(Func<S, Eps> deleg)
        {
            deleg(default(S));
            return default(SC);
        }
    }

    public class Eps : Session { }
}
