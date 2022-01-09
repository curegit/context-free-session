using System;
using System.Collections;
using System.Collections.Generic;

namespace ContextFreeSession
{
    [Serializable]
    internal class OrderedSet<T> : IEnumerable<T>, IEquatable<OrderedSet<T>>
    {
        private readonly List<T> sequence = new();

        private readonly HashSet<T> set = new();

        public OrderedSet() { }

        public OrderedSet(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Add(T item)
        {
            if (!set.Contains(item))
            {
                set.Add(item);
                sequence.Add(item);
            }
        }

        public bool Contains(T item)
        {
            return set.Contains(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return sequence.GetEnumerator();
        }

        public override int GetHashCode()
        {
            return set.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as OrderedSet<T>);
        }

        public bool Equals(OrderedSet<T>? other)
        {
            if (other == null) return false;
            return set.SetEquals(other.set);
        }

        public static bool operator ==(OrderedSet<T>? left, OrderedSet<T>? right)
        {
            if (left is null)
            {
                if (right is null)
                {
                    return true;
                }
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(OrderedSet<T>? lhs, OrderedSet<T>? rhs) => !(lhs == rhs);
    }
}
