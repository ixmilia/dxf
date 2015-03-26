// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using IxMilia.Dxf.Tables;

namespace IxMilia.Dxf
{
    public abstract class DxfSymbolTableFlags
    {
        public int StandardFlags;
        public string Name { get; set; }
        protected abstract DxfTableType TableType { get; }
        public string Handle { get; set; }

        public DxfSymbolTableFlags()
        {
        }

        internal void AddCommonValuePairs(List<DxfCodePair> pairs)
        {
            pairs.Add(new DxfCodePair(0, DxfTable.TableTypeToName(TableType)));
            if (TableType == DxfTableType.DimStyle)
            {
                pairs.Add(new DxfCodePair(105, Handle));
            }
            else
            {
                pairs.Add(new DxfCodePair(5, Handle));
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
                    Handle = pair.StringValue;
                    break;
            }
        }

        internal abstract void AddValuePairs(List<DxfCodePair> pairs);

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
    }
}
