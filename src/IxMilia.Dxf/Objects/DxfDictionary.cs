// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfDictionary :
        IDictionary<string, IDxfItem>,
        IDxfItemInternal
    {
        private IDictionary<string, DxfPointer> _items = new Dictionary<string, DxfPointer>();
        private string _lastEntryName;

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            base.AddValuePairs(pairs, version, outputHandles);
            pairs.Add(new DxfCodePair(100, "AcDbDictionary"));
            if (version >= DxfAcadVersion.R2000 && this.IsHardOwner != false)
            {
                pairs.Add(new DxfCodePair(280, BoolShort(this.IsHardOwner)));
            }

            if (version >= DxfAcadVersion.R2000)
            {
                pairs.Add(new DxfCodePair(281, (short)(this.DuplicateRecordHandling)));
            }

            var code = IsHardOwner ? 360 : 350;
            foreach (var item in _items.OrderBy(kvp => kvp.Key))
            {
                pairs.Add(new DxfCodePair(3, item.Key));
                pairs.Add(new DxfCodePair(code, UIntHandle(item.Value.Handle)));
            }
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
            foreach (var child in GetChildren())
            {
                if (writtenItems.Add(child))
                {
                    pairs.AddRange(((DxfObject)child).GetValuePairs(version, outputHandles, writtenItems));
                }
            }
        }

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 3:
                    _lastEntryName = pair.StringValue;
                    break;
                case 280:
                    this.IsHardOwner = BoolShort(pair.ShortValue);
                    break;
                case 281:
                    this.DuplicateRecordHandling = (DxfDictionaryDuplicateRecordHandling)(pair.ShortValue);
                    break;
                case 350:
                case 360:
                    Debug.Assert(_lastEntryName != null);
                    var handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                    _items[_lastEntryName] = new DxfPointer(handle);
                    _lastEntryName = null;
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }

        public IEnumerable<IDxfItem> GetChildren()
        {
            return _items.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value.Item);
        }

        IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()
        {
            return _items.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value);
        }

        #region IDictionary implementation

        public IDxfItem this[string key]
        {
            get { return _items[key].Item; }
            set { _items[key] = new DxfPointer(value); }
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public ICollection<string> Keys => _items.Keys;

        public ICollection<IDxfItem> Values => _items.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value.Item).ToList();

        public void Add(KeyValuePair<string, IDxfItem> item) => _items.Add(new KeyValuePair<string, DxfPointer>(item.Key, new DxfPointer(item.Value)));

        public void Add(string key, IDxfItem value) => _items.Add(key, new DxfPointer(value));

        public void Clear() => _items.Clear();

        public bool Contains(KeyValuePair<string, IDxfItem> item) => _items.ContainsKey(item.Key) && _items[item.Key].Item == item.Value;

        public bool ContainsKey(string key) => _items.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, IDxfItem>[] array, int arrayIndex)
        {
            foreach (var value in _items)
            {
                array[arrayIndex++] = new KeyValuePair<string, IDxfItem>(value.Key, value.Value.Item);
            }
        }

        public IEnumerator<KeyValuePair<string, IDxfItem>> GetEnumerator() => new Enumerator(_items);

        public bool Remove(KeyValuePair<string, IDxfItem> item)
        {
            DxfPointer pointer;
            if (_items.TryGetValue(item.Key, out pointer) && pointer.Item == item.Value)
            {
                _items.Remove(item.Key);
                return true;
            }

            return false;
        }

        public bool Remove(string key) => _items.Remove(key);

        public bool TryGetValue(string key, out IDxfItem value)
        {
            if (_items.ContainsKey(key))
            {
                value = _items[key].Item;
                return true;
            }
            else
            {
                value = default(IDxfItem);
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();

        private struct Enumerator : IEnumerator<KeyValuePair<string, IDxfItem>>
        {
            IEnumerator<KeyValuePair<string, DxfPointer>> _enumerator;

            public Enumerator(IEnumerable<KeyValuePair<string, DxfPointer>> items)
                : this()
            {
                _enumerator = items.GetEnumerator();
            }

            public KeyValuePair<string, IDxfItem> Current => new KeyValuePair<string, IDxfItem>(_enumerator.Current.Key, _enumerator.Current.Value.Item);

            object IEnumerator.Current => Current;

            public void Dispose() => _enumerator.Dispose();

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();
        }

        #endregion
    }
}
