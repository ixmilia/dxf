// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using IxMilia.Dxf.Sections;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf.Tables
{
    public abstract partial class DxfTable
    {
        public const string AppIdText = "APPID";
        public const string BlockRecordText = "BLOCK_RECORD";
        public const string DimStyleText = "DIMSTYLE";
        public const string LayerText = "LAYER";
        public const string LTypeText = "LTYPE";
        public const string StyleText = "STYLE";
        public const string UcsText = "UCS";
        public const string ViewText = "VIEW";
        public const string ViewPortText = "VPORT";

        internal abstract DxfTableType TableType { get; }
        public string Handle { get; set; }
        public int MaxEntries { get; set; }
        public string OwnerHandle { get; set; }

        public DxfTable()
        {
        }

        protected abstract IEnumerable<DxfSymbolTableFlags> GetSymbolItems();

        internal IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version)
        {
            var pairs = new List<DxfCodePair>();
            var symbolItems = GetSymbolItems();
            if (!symbolItems.Any())
                return pairs;

            // common pairs
            pairs.Add(new DxfCodePair(0, DxfSection.TableText));
            pairs.Add(new DxfCodePair(2, TableTypeToName(TableType)));
            if (version >= DxfAcadVersion.R13)
            {
                pairs.Add(new DxfCodePair(5, Handle));
                // 102 ({ACAD_XDICTIONARY) codes surrounding code 360
                if (version >= DxfAcadVersion.R2000)
                    pairs.Add(new DxfCodePair(330, OwnerHandle));
                pairs.Add(new DxfCodePair(100, "AcDbSymbolTable"));
            }

            pairs.Add(new DxfCodePair(70, (short)MaxEntries));

            foreach (var item in symbolItems.OrderBy(i => i.Name))
            {
                item.AddCommonValuePairs(pairs, version);
                item.AddValuePairs(pairs, version);
            }

            pairs.Add(new DxfCodePair(0, DxfSection.EndTableText));
            return pairs;
        }

        public string TableTypeName
        {
            get { return TableTypeToName(TableType); }
        }

        public static DxfTableType TableNameToType(string name)
        {
            var type = DxfTableType.AppId;
            switch (name)
            {
                case AppIdText:
                    type = DxfTableType.AppId;
                    break;
                case BlockRecordText:
                    type = DxfTableType.BlockRecord;
                    break;
                case DimStyleText:
                    type = DxfTableType.DimStyle;
                    break;
                case LayerText:
                    type = DxfTableType.Layer;
                    break;
                case LTypeText:
                    type = DxfTableType.LType;
                    break;
                case StyleText:
                    type = DxfTableType.Style;
                    break;
                case UcsText:
                    type = DxfTableType.Ucs;
                    break;
                case ViewText:
                    type = DxfTableType.View;
                    break;
                case ViewPortText:
                    type = DxfTableType.ViewPort;
                    break;
            }
            return type;
        }

        public static string TableTypeToName(DxfTableType type)
        {
            string name = "NONE";
            switch (type)
            {
                case DxfTableType.AppId:
                    name = AppIdText;
                    break;
                case DxfTableType.BlockRecord:
                    name = BlockRecordText;
                    break;
                case DxfTableType.DimStyle:
                    name = DimStyleText;
                    break;
                case DxfTableType.Layer:
                    name = LayerText;
                    break;
                case DxfTableType.LType:
                    name = LTypeText;
                    break;
                case DxfTableType.Style:
                    name = StyleText;
                    break;
                case DxfTableType.Ucs:
                    name = UcsText;
                    break;
                case DxfTableType.View:
                    name = ViewText;
                    break;
                case DxfTableType.ViewPort:
                    name = ViewPortText;
                    break;
            }
            return name;
        }

        internal static DxfTable FromBuffer(DxfCodePairBufferReader buffer)
        {
            var pair = buffer.Peek();
            buffer.Advance();
            if (pair.Code != 2)
            {
                throw new DxfReadException("Expected table type.");
            }

            var tableType = pair.StringValue;

            // read common table values
            var commonValues = new List<DxfCodePair>();
            pair = buffer.Peek();
            while (pair != null && pair.Code != 0)
            {
                commonValues.Add(pair);
                buffer.Advance();
                pair = buffer.Peek();
            }

            DxfTable result;
            switch (tableType)
            {
                case DxfTable.DimStyleText:
                    result = DxfDimStyleTable.ReadFromBuffer(buffer);
                    break;
                case DxfTable.LayerText:
                    result = DxfLayerTable.ReadFromBuffer(buffer);
                    break;
                case DxfTable.LTypeText:
                    result = DxfLTypeTable.ReadFromBuffer(buffer);
                    break;
                case DxfTable.StyleText:
                    result = DxfStyleTable.ReadFromBuffer(buffer);
                    break;
                case DxfTable.ViewPortText:
                    result = DxfViewPortTable.ReadFromBuffer(buffer);
                    break;
                default:
                    SwallowTable(buffer);
                    result = null;
                    break;
            }

            if (result != null)
            {
                // set common values
                foreach (var common in commonValues)
                {
                    switch (common.Code)
                    {
                        case 5:
                            result.Handle = common.StringValue;
                            break;
                        case 70:
                            result.MaxEntries = common.ShortValue;
                            break;
                    }
                }
            }

            return result;
        }

        internal static void SwallowTable(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                buffer.Advance();
                if (DxfTablesSection.IsTableEnd(pair))
                    break;
            }
        }
    }
}
