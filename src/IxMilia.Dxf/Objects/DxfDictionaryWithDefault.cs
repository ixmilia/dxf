using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfDictionaryWithDefault :
        IDictionary<string, IDxfItem>,
        IDxfItemInternal
    {
        private IDictionary<string, DxfPointer> _items = new Dictionary<string, DxfPointer>();
        private string _lastEntryName;
        internal readonly DxfPointer DefaultObjectPointer = new DxfPointer();

        public DxfObject DefaultObject
        {
            get { return DefaultObjectPointer.Item as DxfObject; }
            set { DefaultObjectPointer.Item = value; }
        }

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, bool writeXData)
        {
            base.AddValuePairs(pairs, version, outputHandles, writeXData: false);
            pairs.Add(new DxfCodePair(100, "AcDbDictionary"));

            if (version >= DxfAcadVersion.R2000)
            {
                pairs.Add(new DxfCodePair(281, (short)(this.DuplicateRecordHandling)));
            }

            foreach (var item in _items.OrderBy(kvp => kvp.Key))
            {
                pairs.Add(new DxfCodePair(3, item.Key));
                pairs.Add(new DxfCodePair(350, UIntHandle(item.Value.Handle)));
            }


            if (version >= DxfAcadVersion.R2000)
            {
                pairs.Add(new DxfCodePair(100, "AcDbDictionaryWithDefault"));
                if (DefaultObject != null && DefaultObjectPointer.Handle != 0u)
                {
                    pairs.Add(new DxfCodePair(340, UIntHandle(DefaultObjectPointer.Handle)));
                }
            }

            if (writeXData)
            {
                DxfXData.AddValuePairs(XData, pairs, version, outputHandles);
            }
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
            if (DefaultObject != null && writtenItems.Add(DefaultObject))
            {
                pairs.AddRange(DefaultObject.GetValuePairs(version, outputHandles, writtenItems));
            }

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
                case 281:
                    this.DuplicateRecordHandling = (DxfDictionaryDuplicateRecordHandling)(pair.ShortValue);
                    break;
                case 340:
                    this.DefaultObjectPointer.Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
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
            yield return DefaultObjectPointer;
            foreach (var pointer in _items.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value))
            {
                yield return pointer;
            }
        }

        #region IDictionary implementation

        public IDxfItem this[string key]
        {
            get { return _items.ContainsKey(key) ? _items[key].Item : DefaultObject; }
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
            }
            else
            {
                value = DefaultObject;
            }

            return true;
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
