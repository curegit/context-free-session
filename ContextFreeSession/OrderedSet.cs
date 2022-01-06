using System;
using System.Collections;
using System.Collections.Generic;

namespace ContextFreeSession
{
    [Serializable]
    internal class OrderedSet<T> : IEnumerable<T>
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
    }
}
