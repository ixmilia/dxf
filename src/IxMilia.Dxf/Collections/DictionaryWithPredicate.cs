using System;
using System.Collections;
using System.Collections.Generic;

namespace IxMilia.Dxf.Collections
{
    internal class DictionaryWithPredicate<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();

        public Func<TKey, TValue, bool> ItemPredicate { get; }

        public DictionaryWithPredicate(Func<TKey, TValue, bool> itemPredicate)
        {
            if (itemPredicate == null)
            {
                throw new ArgumentNullException(nameof(itemPredicate));
            }

            ItemPredicate = itemPredicate;
        }

        private void ValidatePredicate(TKey key, TValue value)
        {
            if (!ItemPredicate(key, value))
            {
                throw new InvalidOperationException("Item does not meet the criteria to be added to this collection.");
            }
        }

        public TValue this[TKey key]
        {
            get => ((IDictionary<TKey, TValue>)_dict)[key];
            set
            {
                ValidatePredicate(key, value);
                ((IDictionary<TKey, TValue>)_dict)[key] = value;
            }
        }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)_dict).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)_dict).Values;

        public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            ValidatePredicate(key, value);
            ((IDictionary<TKey, TValue>)_dict).Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ValidatePredicate(item.Key, item.Value);
            ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_dict).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)_dict).GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_dict).Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Remove(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)_dict).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_dict).GetEnumerator();
        }
    }
}
