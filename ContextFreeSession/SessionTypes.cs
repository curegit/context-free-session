using System;
using System.Linq;
using System.ComponentModel;

namespace ContextFreeSession.Runtime
{
    public interface IRole { }

    public interface ILabel { }

    public interface IStart
    {
        public string Role { get; }
    }

    public abstract class Session
    {
        internal Session() { }

        internal bool used;

        private ICommunicator? communicator;

        internal ICommunicator Communicator
        {
            get
            {
                return communicator ?? throw new InvalidOperationException();
            }
            set
            {
                communicator = value;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string? ToString()
        {
            return base.ToString();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }

    public class SendSession<To, L, T, S> : Session where To : IRole where L : ILabel where S : Session
    {
        public S Send<to, label>(T value) where to : To where label : L
        {
            if (used) throw new LinearityViolationException();
            used = true;
            var toString = typeof(to).Name;
            var labelString = typeof(label).Name;
            Communicator.Send(toString, labelString, value);
            var session = (S)Activator.CreateInstance(typeof(S), true)!;
            session.Communicator = Communicator;
            return session;
        }
    }

    public class SendSession<To, L1, T1, S1, L2, T2, S2> : Session where To : IRole where L1 : ILabel where S1 : Session where L2 : ILabel where S2 : Session
    {
        public S1 Send<to, label>(T1 value) where to : To where label : L1
        {
            if (used) throw new LinearityViolationException();
            used = true;
            var toString = typeof(to).Name;
            var labelString = typeof(label).Name;
            Communicator.Send(toString, labelString, value);
            var session = (S1)Activator.CreateInstance(typeof(S1), true)!;
            session.Communicator = Communicator;
            return session;
        }

        public S2 Send<to, label>(T2 value) where to : To where label : L2
        {
            if (used) throw new LinearityViolationException();
            used = true;
            var toString = typeof(to).Name;
            var labelString = typeof(label).Name;
            Communicator.Send(toString, labelString, value);
            var session = (S2)Activator.CreateInstance(typeof(S2), true)!;
            session.Communicator = Communicator;
            return session;
        }
    }

    public class SendSession<To, L1, T1, S1, L2, T2, S2, L3, T3, S3> : Session where To : IRole where L1 : ILabel where S1 : Session where L2 : ILabel where S2 : Session where L3 : ILabel where S3 : Session
    {
        public S1 Send<to, label>(T1 value) where to : To where label : L1
        {
            if (used) throw new LinearityViolationException();
            used = true;
            var toString = typeof(to).Name;
            var labelString = typeof(label).Name;
            Communicator.Send(toString, labelString, value);
            var session = (S1)Activator.CreateInstance(typeof(S1), true)!;
            session.Communicator = Communicator;
            return session;
        }

        public S2 Send<to, label>(T2 value) where to : To where label : L2
        {
            if (used) throw new LinearityViolationException();
            used = true;
            var toString = typeof(to).Name;
            var labelString = typeof(label).Name;
            Communicator.Send(toString, labelString, value);
            var session = (S2)Activator.CreateInstance(typeof(S2), true)!;
            session.Communicator = Communicator;
            return session;
        }

        public S3 Send<to, label>(T3 value) where to : To where label : L3
        {
            if (used) throw new LinearityViolationException();
            used = true;
            var toString = typeof(to).Name;
            var labelString = typeof(label).Name;
            Communicator.Send(toString, labelString, value);
            var session = (S3)Activator.CreateInstance(typeof(S3), true)!;
            session.Communicator = Communicator;
            return session;
        }
    }

    public class ReceiveSession<From, L, T, S> : Session where L : ILabel where S : Session
    {
        public S Receive<from, label>(out T result) where from : From where label : L
        {
            if (used) throw new LinearityViolationException();
            used = true;
            var fromString = typeof(from).Name;
            var labelString = typeof(label).Name;
            (var l, result) = Communicator.Receive<T>(fromString, labelString);
            if (l != labelString) throw new UnexpectedLabelException();
            var session = (S)Activator.CreateInstance(typeof(S), true)!;
            session.Communicator = Communicator;
            return session;
        }
    }

    internal static class BranchUtility
    {
        public static bool ContainsLabel<LS>(string label)
        {
            var type = typeof(LS);
            if (type.IsGenericType)
            {
                var elementTypes = type.GetGenericArguments();
                var tupleList = elementTypes.Take(7).ToList();
                while (elementTypes.Length == 8)
                {
                    elementTypes = elementTypes[7].GetGenericArguments();
                    tupleList.AddRange(elementTypes.Take(7));
                }
                return tupleList.Any(x => x.Name == label);
            }
            else
            {
                return label == typeof(LS).Name;
            }
        }
    }

    public class BranchSession<From, LS1, S1, LS2, S2> : Session where From : IRole where S1 : Session where S2 : Session
    {
        public Eps Branch<from, labels1, labels2>(Func<S1, Eps> f1, Func<S2, Eps> f2) where from : From where labels1 : LS1 where labels2 : LS2
        {
            ArgumentNullException.ThrowIfNull(f1);
            ArgumentNullException.ThrowIfNull(f2);
            if (used) throw new LinearityViolationException();
            used = true;
            var fromString = typeof(from).Name;
            var label = Communicator.Peek(fromString);
            if (BranchUtility.ContainsLabel<labels1>(label))
            {
                var session = (S1)Activator.CreateInstance(typeof(S1), true)!;
                session.Communicator = Communicator;
                return f1(session);
            }
            if (BranchUtility.ContainsLabel<labels2>(label))
            {
                var session = (S2)Activator.CreateInstance(typeof(S2), true)!;
                session.Communicator = Communicator;
                return f2(session);
            }
            throw new UnexpectedLabelException();
        }

        public (Eps, T) BranchFunc<from, labels1, labels2, T>(Func<S1, (Eps, T)> f1, Func<S2, (Eps, T)> f2) where from : From where labels1 : LS1 where labels2 : LS2
        {
            ArgumentNullException.ThrowIfNull(f1);
            ArgumentNullException.ThrowIfNull(f2);
            if (used) throw new LinearityViolationException();
            used = true;
            var fromString = typeof(from).Name;
            var label = Communicator.Peek(fromString);
            if (BranchUtility.ContainsLabel<labels1>(label))
            {
                var session = (S1)Activator.CreateInstance(typeof(S1), true)!;
                session.Communicator = Communicator;
                return f1(session);
            }
            if (BranchUtility.ContainsLabel<labels2>(label))
            {
                var session = (S2)Activator.CreateInstance(typeof(S2), true)!;
                session.Communicator = Communicator;
                return f2(session);
            }
            throw new UnexpectedLabelException();
        }
    }

    public class BranchSession<From, LS1, S1, LS2, S2, LS3, S3> : Session where From : IRole where S1 : Session where S2 : Session where S3 : Session
    {
        public Eps Branch<from, labels1, labels2, labels3>(Func<S1, Eps> f1, Func<S2, Eps> f2, Func<S3, Eps> f3) where from : From where labels1 : LS1 where labels2 : LS2 where labels3 : LS3
        {
            ArgumentNullException.ThrowIfNull(f1);
            ArgumentNullException.ThrowIfNull(f2);
            ArgumentNullException.ThrowIfNull(f3);
            if (used) throw new LinearityViolationException();
            used = true;
            var fromString = typeof(from).Name;
            var label = Communicator.Peek(fromString);
            if (BranchUtility.ContainsLabel<labels1>(label))
            {
                var session = (S1)Activator.CreateInstance(typeof(S1), true)!;
                session.Communicator = Communicator;
                return f1(session);
            }
            if (BranchUtility.ContainsLabel<labels2>(label))
            {
                var session = (S2)Activator.CreateInstance(typeof(S2), true)!;
                session.Communicator = Communicator;
                return f2(session);
            }
            if (BranchUtility.ContainsLabel<labels3>(label))
            {
                var session = (S3)Activator.CreateInstance(typeof(S3), true)!;
                session.Communicator = Communicator;
                return f3(session);
            }
            throw new UnexpectedLabelException();
        }

        public (Eps, T) BranchFunc<from, labels1, labels2, labels3, T>(Func<S1, (Eps, T)> f1, Func<S2, (Eps, T)> f2, Func<S3, (Eps, T)> f3) where from : From where labels1 : LS1 where labels2 : LS2 where labels3 : LS3
        {
            ArgumentNullException.ThrowIfNull(f1);
            ArgumentNullException.ThrowIfNull(f2);
            ArgumentNullException.ThrowIfNull(f3);
            if (used) throw new LinearityViolationException();
            used = true;
            var fromString = typeof(from).Name;
            var label = Communicator.Peek(fromString);
            if (BranchUtility.ContainsLabel<labels1>(label))
            {
                var session = (S1)Activator.CreateInstance(typeof(S1), true)!;
                session.Communicator = Communicator;
                return f1(session);
            }
            if (BranchUtility.ContainsLabel<labels2>(label))
            {
                var session = (S2)Activator.CreateInstance(typeof(S2), true)!;
                session.Communicator = Communicator;
                return f2(session);
            }
            if (BranchUtility.ContainsLabel<labels3>(label))
            {
                var session = (S3)Activator.CreateInstance(typeof(S3), true)!;
                session.Communicator = Communicator;
                return f3(session);
            }
            throw new UnexpectedLabelException();
        }
    }

    public class Call<S, C> : Session where S : Session where C : Session
    {
        public C Do(Func<S, Eps> deleg)
        {
            ArgumentNullException.ThrowIfNull(deleg);
            if (used) throw new LinearityViolationException();
            used = true;
            var inner = (S)Activator.CreateInstance(typeof(S), true)!;
            inner.Communicator = Communicator;
            var eps = deleg(inner);
            if (eps.Communicator != Communicator) throw new ResumptionException();
            if (eps.used) throw new LinearityViolationException();
            eps.used = true;
            var session = (C)Activator.CreateInstance(typeof(C), true)!;
            session.Communicator = eps.Communicator;
            return session;
        }

        public C Do<T>(Func<S, T, Eps> deleg, T args)
        {
            ArgumentNullException.ThrowIfNull(deleg);
            if (used) throw new LinearityViolationException();
            used = true;
            var inner = (S)Activator.CreateInstance(typeof(S), true)!;
            inner.Communicator = Communicator;
            var eps = deleg(inner, args);
            if (eps.Communicator != Communicator) throw new ResumptionException();
            if (eps.used) throw new LinearityViolationException();
            eps.used = true;
            var session = (C)Activator.CreateInstance(typeof(C), true)!;
            session.Communicator = eps.Communicator;
            return session;
        }

        public (C, TResult) DoFunc<TResult>(Func<S, (Eps, TResult)> deleg)
        {
            ArgumentNullException.ThrowIfNull(deleg);
            if (used) throw new LinearityViolationException();
            used = true;
            var inner = (S)Activator.CreateInstance(typeof(S), true)!;
            inner.Communicator = Communicator;
            var (eps, result) = deleg(inner);
            if (eps.Communicator != Communicator) throw new ResumptionException();
            if (eps.used) throw new LinearityViolationException();
            eps.used = true;
            var session = (C)Activator.CreateInstance(typeof(C), true)!;
            session.Communicator = eps.Communicator;
            return (session, result);
        }

        public (C, TResult) DoFunc<T, TResult>(Func<S, T, (Eps, TResult)> deleg, T args)
        {
            ArgumentNullException.ThrowIfNull(deleg);
            if (used) throw new LinearityViolationException();
            used = true;
            var inner = (S)Activator.CreateInstance(typeof(S), true)!;
            inner.Communicator = Communicator;
            var (eps, result) = deleg(inner, args);
            if (eps.Communicator != Communicator) throw new ResumptionException();
            if (eps.used) throw new LinearityViolationException();
            eps.used = true;
            var session = (C)Activator.CreateInstance(typeof(C), true)!;
            session.Communicator = eps.Communicator;
            return (session, result);
        }
    }

    public class Eps : Session
    {
        public void Close()
        {
            if (used) throw new LinearityViolationException();
            used = true;
            Communicator.Close();
        }
    }
}
