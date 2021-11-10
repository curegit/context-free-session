using System;
using System.Diagnostics.CodeAnalysis;

namespace ContextFreeSession
{
    [Serializable]
    public struct Unit : IEquatable<Unit>, IComparable, IComparable<Unit>
    {
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Unit;
        }

        public bool Equals(Unit other)
        {
            return true;
        }

        int IComparable.CompareTo(object? other)
        {
            if (other is null)
            {
                return 1;
            }
            else if (other is not Unit)
            {
                throw new ArgumentException(null, nameof(other));
            }
            else
            {
                return 0;
            }
        }

        public int CompareTo(Unit other)
        {
            return 0;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "()";
        }

        public static bool operator ==(Unit left, Unit right)
        {
            return true;
        }

        public static bool operator !=(Unit left, Unit right)
        {
            return false;
        }
    }
}
