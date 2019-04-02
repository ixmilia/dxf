// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Collections;
using IxMilia.Dxf.Tables;

namespace IxMilia.Dxf
{
    public abstract class DxfSymbolTableFlags : IDxfItemInternal
    {
        public IDxfItem Owner { get; private set; }
        void IDxfItemInternal.SetOwner(IDxfItem owner)
        {
            Owner = owner;
        }
        IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()
        {
            yield break;
        }

        IEnumerable<IDxfItemInternal> IDxfItemInternal.GetChildItems()
        {
            yield break;
        }

        public int StandardFlags;
        public string Name { get; set; }
        protected abstract DxfTableType TableType { get; }
        public uint Handle { get; set; }
        public uint OwnerHandle { get; set; }
        public IList<DxfCodePairGroup> ExtensionDataGroups { get; }

        public DxfSymbolTableFlags()
        {
            ExtensionDataGroups = new ListNonNull<DxfCodePairGroup>();
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

        protected static double EnsurePositiveOrDefault(double value, double defaultValue)
        {
            return DxfCommonConverters.EnsurePositiveOrDefault(value, defaultValue);
        }

        protected static int EnsurePositiveOrDefault(int value, int defaultValue)
        {
            return DxfCommonConverters.EnsurePositiveOrDefault(value, defaultValue);
        }
    }

    public partial class DxfBlockRecord
    {
        public byte[] BitmapData { get; set; }

        internal override void BeforeWrite()
        {
            _bitmapPreviewData.Clear();
            var str = DxfCommonConverters.HexBytes(BitmapData);
            foreach (var line in DxfCommonConverters.SplitIntoLines(str))
            {
                _bitmapPreviewData.Add(line);
            }
        }

        internal override void AfterRead()
        {
            var hex = string.Join(string.Empty, _bitmapPreviewData.ToArray());
            _bitmapPreviewData.Clear(); // don't keep this around
            BitmapData = DxfCommonConverters.HexBytes(hex);
        }
    }

    public partial class DxfLayer
    {
        public bool IsLayerOn { get; set; } = true;

        private DxfColor ReadColorValue(short value)
        {
            IsLayerOn = value >= 0;
            return DxfColor.FromRawValue(Math.Abs(value));
        }

        private short GetWritableColorValue(DxfColor color)
        {
            var value = Math.Abs(color?.RawValue ?? 7);
            if (value == 0 || value == 256)
            {
                // BYLAYER and BYBLOCK aren't valid colors
                value = 7;
            }

            return IsLayerOn
                ? value
                : (short)-value;
        }

        private string GetWritableLineTypeName(string lineTypeName)
        {
            return Compat.IsNullOrWhiteSpace(lineTypeName) ? "CONTINUOUS" : lineTypeName;
        }
    }
}
