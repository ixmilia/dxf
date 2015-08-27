// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfDictionary : IDictionary<string, uint>
    {
        private IDictionary<string, uint> _entries = new Dictionary<string, uint>();

        protected override DxfObject PostParse()
        {
            Debug.Assert(_entryNames.Count == _entryHandles.Count);
            var count = Math.Min(_entryNames.Count, _entryHandles.Count);
            for (int i = 0; i < count; i++)
            {
                _entries[_entryNames[i]] = _entryHandles[i];
            }

            _entryNames.Clear();
            _entryHandles.Clear();

            return this;
        }

        #region IDictionary implementation

        public uint this[string key]
        {
            get { return _entries[key]; }
            set { _entries[key] = value; }
        }

        public int Count => _entries.Count;

        public bool IsReadOnly => false;

        public ICollection<string> Keys => _entries.Keys;

        public ICollection<uint> Values => _entries.Values;

        public void Add(KeyValuePair<string, uint> item) => _entries.Add(item);

        public void Add(string key, uint value) => _entries.Add(key, value);

        public void Clear() => _entries.Clear();

        public bool Contains(KeyValuePair<string, uint> item) => _entries.Contains(item);

        public bool ContainsKey(string key) => _entries.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, uint>[] array, int arrayIndex) => _entries.CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<string, uint>> GetEnumerator() => _entries.GetEnumerator();

        public bool Remove(KeyValuePair<string, uint> item) => _entries.Remove(item);

        public bool Remove(string key) => _entries.Remove(key);

        public bool TryGetValue(string key, out uint value) => _entries.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_entries).GetEnumerator();

        #endregion
    }
}
