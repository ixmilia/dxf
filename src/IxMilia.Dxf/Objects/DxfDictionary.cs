// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfDictionary : IDxfHasChildrenWithHandle, IDictionary<string, string>
    {
        internal Dictionary<string, uint> Handles { get; } = new Dictionary<string, uint>();
        private IDictionary<string, string> _values = new Dictionary<string, string>();
        private string _lastEntryName;
        private List<DxfDictionaryVariable> _children = new List<DxfDictionaryVariable>();
        private bool _childrenAreDirty;

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            PopulateHandles();

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
            foreach (var item in Handles)
            {
                pairs.Add(new DxfCodePair(3, item.Key));
                pairs.Add(new DxfCodePair(code, UIntHandle(item.Value)));
            }
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var child in ((IDxfHasChildrenWithHandle)this).GetChildren().Cast<DxfDictionaryVariable>())
            {
                pairs.AddRange(child.GetValuePairs(version, outputHandles));
            }
        }

        private void PopulateHandles()
        {
            Handles.Clear();
            var children = ((IDxfHasChildrenWithHandle)this).GetChildren().Cast<DxfDictionaryVariable>().ToList();
            var keys = _values.Keys.OrderBy(k => k).ToList();
            Debug.Assert(children.Count == keys.Count);
            foreach (var pair in keys.Zip(children, Tuple.Create))
            {
                Handles[pair.Item1] = pair.Item2.Handle;
                pair.Item2.OwnerHandle = this.Handle;
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
                    Handles[_lastEntryName] = handle;
                    _lastEntryName = null;
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }

        IEnumerable<IDxfHasHandle> IDxfHasChildrenWithHandle.GetChildren()
        {
            if (_childrenAreDirty)
            {
                _childrenAreDirty = false;
                _children.Clear();
                foreach (var kvp in _values.OrderBy(k => k.Key))
                {
                    _children.Add(new DxfDictionaryVariable() { Value = kvp.Value });
                }
            }

            return _children;
        }

        #region IDictionary implementation

        public string this[string key]
        {
            get { return _values[key]; }
            set
            {
                _childrenAreDirty = true;
                _values[key] = value;
            }
        }

        public int Count => _values.Count;

        public bool IsReadOnly => false;

        public ICollection<string> Keys => _values.Keys;

        public ICollection<string> Values => _values.Values;

        public void Add(KeyValuePair<string, string> item)
        {
            _childrenAreDirty = true;
            _values.Add(item);
        }

        public void Add(string key, string value)
        {
            _childrenAreDirty = true;
            _values.Add(key, value);
        }

        public void Clear()
        {
            _childrenAreDirty = true;
            _values.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item) => _values.Contains(item);

        public bool ContainsKey(string key) => _values.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();

        public bool Remove(KeyValuePair<string, string> item)
        {
            _childrenAreDirty = true;
            return _values.Remove(item);
        }

        public bool Remove(string key)
        {
            _childrenAreDirty = true;
            return _values.Remove(key);
        }

        public bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();

        #endregion
    }
}
