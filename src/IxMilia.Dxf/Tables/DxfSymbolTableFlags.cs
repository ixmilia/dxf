// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using IxMilia.Dxf.Tables;

namespace IxMilia.Dxf
{
    public abstract class DxfSymbolTableFlags : IDxfHasHandle
    {
        public int StandardFlags;
        public string Name { get; set; }
        protected abstract DxfTableType TableType { get; }
        public uint Handle { get; set; }
        public uint OwnerHandle { get; set; }
        public List<DxfCodePairGroup> ExtensionDataGroups { get; private set; }

        public DxfSymbolTableFlags()
        {
            ExtensionDataGroups = new List<DxfCodePairGroup>();
        }

        internal void AddCommonValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            pairs.Add(new DxfCodePair(0, DxfTable.TableTypeToName(TableType)));
            if (outputHandles)
            {
                int code = TableType == DxfTableType.DimStyle ? 105 : 5;
                pairs.Add(new DxfCodePair(code, DxfCommonConverters.UIntHandle(Handle)));
            }

            foreach (var group in ExtensionDataGroups)
            {
                group.AddValuePairs(pairs, version, outputHandles);
            }

            if (version >= DxfAcadVersion.R2000)
            {
                pairs.Add(new DxfCodePair(330, DxfCommonConverters.UIntHandle(OwnerHandle)));
            }

            pairs.Add(new DxfCodePair(100, "AcDbSymbolTableRecord"));
        }

        internal void TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 2:
                    Name = pair.StringValue;
                    break;
                case 5:
                    Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                    break;
                case 330:
                    OwnerHandle = DxfCommonConverters.UIntHandle(pair.StringValue);
                    break;
            }
        }

        internal abstract void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles);

        internal virtual void BeforeWrite()
        {
        }

        internal virtual void AfterRead()
        {
        }

        public bool ExternallyDependentOnXRef
        {
            get { return DxfHelpers.GetFlag(StandardFlags, 16); }
            set { DxfHelpers.SetFlag(value, ref StandardFlags, 16); }
        }

        public bool ExternallyDependentXRefResolved
        {
            get { return ExternallyDependentOnXRef && DxfHelpers.GetFlag(StandardFlags, 32); }
            set
            {
                ExternallyDependentOnXRef = true;
                DxfHelpers.SetFlag(value, ref StandardFlags, 32);
            }
        }

        public bool ReferencedOnLastEdit
        {
            get { return DxfHelpers.GetFlag(StandardFlags, 64); }
            set { DxfHelpers.SetFlag(value, ref StandardFlags, 64); }
        }

        protected static bool BoolShort(short s)
        {
            return s != 0;
        }

        protected static short BoolShort(bool b)
        {
            return (short)(b ? 1 : 0);
        }

        protected static uint UIntHandle(string s)
        {
            return DxfCommonConverters.UIntHandle(s);
        }

        protected static string UIntHandle(uint u)
        {
            return DxfCommonConverters.UIntHandle(u);
        }
    }

    public partial class DxfBlockRecord
    {
        public byte[] BitmapData { get; set; }

        internal override void BeforeWrite()
        {
            _bitmapPreviewData.Clear();
            var str = DxfCommonConverters.HexBytes(BitmapData);
            _bitmapPreviewData.AddRange(DxfCommonConverters.SplitIntoLines(str));
        }

        internal override void AfterRead()
        {
            var hex = string.Join(string.Empty, _bitmapPreviewData);
            _bitmapPreviewData.Clear(); // don't keep this around
            BitmapData = DxfCommonConverters.HexBytes(hex);
        }
    }

    public partial class DxfLayer
    {
        private void ValidateColor(DxfColor value)
        {
            // null is OK
            if (!value?.IsIndex ?? false)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Layer colors must be an indexable value: [1, 255]");
            }
        }
    }
}
