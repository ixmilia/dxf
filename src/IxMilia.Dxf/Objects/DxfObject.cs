// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Dxf.Objects
{
    public enum DxfDictionaryDuplicateRecordHandling
    {
        NotApplicable = 0,
        KeepExisting = 1,
        UseClone = 2,
        UpdateXrefAndName = 3,
        UpdateName = 4,
        UnmangleName = 5
    }

    public abstract partial class DxfObject
    {
        protected List<DxfCodePair> ExcessCodePairs = new List<DxfCodePair>();
        protected DxfXData XDataProtected { get; set; }
        public List<DxfCodePairGroup> ExtensionDataGroups { get; private set; }

        public abstract DxfObjectType ObjectType { get; }

        protected virtual DxfAcadVersion MinVersion
        {
            get { return DxfAcadVersion.Min; }
        }

        protected virtual DxfAcadVersion MaxVersion
        {
            get { return DxfAcadVersion.Max; }
        }

        protected DxfObject()
        {
            Initialize();
            ExtensionDataGroups = new List<DxfCodePairGroup>();
        }

        protected virtual void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
        }

        protected virtual DxfObject PostParse()
        {
            return this;
        }

        public IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version, bool outputHandles)
        {
            var pairs = new List<DxfCodePair>();
            if (version >= MinVersion && version <= MaxVersion)
            {
                AddValuePairs(pairs, version, outputHandles);
                AddTrailingCodePairs(pairs, version, outputHandles);
            }

            return pairs;
        }

        private void AddExtensionValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var group in ExtensionDataGroups)
            {
                group.AddValuePairs(pairs, version, outputHandles);
            }
        }

        internal virtual DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }
                else if (pair.Code == DxfCodePairGroup.GroupCodeNumber)
                {
                    buffer.Advance();
                    var groupName = DxfCodePairGroup.GetGroupName(pair.StringValue);
                    ExtensionDataGroups.Add(DxfCodePairGroup.FromBuffer(buffer, groupName));
                }
                else if (pair.Code == (int)DxfXDataType.ApplicationName)
                {
                    XDataProtected = DxfXData.FromBuffer(buffer, pair.StringValue);
                }

                if (!TrySetPair(pair))
                {
                    ExcessCodePairs.Add(pair);
                }

                buffer.Advance();
            }

            return PostParse();
        }

        protected static bool BoolShort(short s)
        {
            return DxfCommonConverters.BoolShort(s);
        }

        protected static short BoolShort(bool b)
        {
            return DxfCommonConverters.BoolShort(b);
        }

        protected static uint UIntHandle(string s)
        {
            return DxfCommonConverters.UIntHandle(s);
        }

        protected static string UIntHandle(uint u)
        {
            return DxfCommonConverters.UIntHandle(u);
        }

        private static void SwallowObject(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                    break;
                buffer.Advance();
            }
        }
    }

    public partial class DxfAcadProxyObject
    {
        public List<string> ObjectIds { get; } = new List<string>();

        public uint DrawingVersion
        {
            get { return _objectDrawingFormat | 0x0000FFFF; }
            set { _objectDrawingFormat |= value & 0x0000FFFF; }
        }

        public uint MaintenenceReleaseVersion
        {
            get { return (_objectDrawingFormat | 0xFFFF0000) >> 16; }
            set { _objectDrawingFormat |= (value & 0xFFFF0000) << 16; }
        }

        protected override DxfObject PostParse()
        {
            ObjectIds.AddRange(_objectIdsA);
            ObjectIds.AddRange(_objectIdsB);
            ObjectIds.AddRange(_objectIdsC);
            ObjectIds.AddRange(_objectIdsD);
            _objectIdsA.Clear();
            _objectIdsB.Clear();
            _objectIdsC.Clear();
            _objectIdsD.Clear();

            return this;
        }
    }

    public partial class DxfAcdbDictionaryWithDefault : IDictionary<string, uint>
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
