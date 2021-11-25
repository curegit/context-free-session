using System;
using System.Collections;
using System.Collections.Generic;

namespace ContextFreeSession
{
    [Serializable]
    internal class AssociationList<TKey, TValue> : IEnumerable<(TKey, TValue)> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> dictionary = new();

        private readonly List<TKey> keys = new();

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
            keys.Add(key);
        }

        public void Insert(int index, TKey key, TValue value)
        {
            if (index < 0 || index > keys.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            else
            {
                dictionary.Add(key, value);
                keys.Insert(index, key);
            }
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public int Count => keys.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<(TKey, TValue)> GetEnumerator()
        {
            foreach (var key in keys)
            {
                yield return (key, dictionary[key]);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                return dictionary[key];
            }
            set
            {
                if (!dictionary.ContainsKey(key))
                {
                    Add(key, value);
                }
                else
                {
                    dictionary[key] = value;
                }
            }
        }
    }
}
