// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using IxMilia.Dxf.Sections;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf.Tables
{
    public enum DxfViewRenderMode
    {
        Classic2D = 0,
        Wireframe = 1,
        HiddenLine = 2,
        FlatShaded = 3,
        GouraudShaded = 4,
        FlatShadedWithWireframe = 5,
        GouraudShadedWithWireframe = 6
    }

    public enum DxfDefaultLightingType
    {
        OneDistantLight = 0,
        TwoDistantLights = 1
    }

    public abstract partial class DxfTable : IDxfHasHandle
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
        public uint Handle { get; set; }
        public uint OwnerHandle { get; set; }
        public List<DxfCodePairGroup> ExtensionDataGroups { get; private set; }

        public DxfTable()
        {
            ExtensionDataGroups = new List<DxfCodePairGroup>();
        }

        protected virtual void Initialize() { }
        protected abstract IEnumerable<DxfSymbolTableFlags> GetSymbolItems();

        internal IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version, bool outputHandles)
        {
            BeforeWrite();

            var pairs = new List<DxfCodePair>();

            // common pairs
            pairs.Add(new DxfCodePair(0, DxfSection.TableText));
            pairs.Add(new DxfCodePair(2, TableTypeToName(TableType)));
            if (outputHandles)
            {
                int code = TableType == DxfTableType.DimStyle ? 105 : 5;
                pairs.Add(new DxfCodePair(code, DxfCommonConverters.UIntHandle(Handle)));
            }

            if (version >= DxfAcadVersion.R13)
            {
                foreach (var group in ExtensionDataGroups)
                {
                    group.AddValuePairs(pairs, version, outputHandles);
                }

                if (version >= DxfAcadVersion.R2000)
                    pairs.Add(new DxfCodePair(330, DxfCommonConverters.UIntHandle(OwnerHandle)));
                pairs.Add(new DxfCodePair(100, "AcDbSymbolTable"));
            }

            var symbolItems = GetSymbolItems().Where(item => item != null).OrderBy(i => i.Name).ToList();
            pairs.Add(new DxfCodePair(70, (short)symbolItems.Count));

            foreach (var item in symbolItems)
            {
                item.AddCommonValuePairs(pairs, version, outputHandles);
                item.AddValuePairs(pairs, version, outputHandles);
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
                throw new DxfReadException("Expected table type.", pair);
            }

            var tableType = pair.StringValue;

            // read common table values
            var commonValues = new List<DxfCodePair>();
            var groups = new List<DxfCodePairGroup>();
            pair = buffer.Peek();
            while (pair != null && pair.Code != 0)
            {
                buffer.Advance();
                if (pair.Code == DxfCodePairGroup.GroupCodeNumber)
                {
                    var groupName = DxfCodePairGroup.GetGroupName(pair.StringValue);
                    groups.Add(DxfCodePairGroup.FromBuffer(buffer, groupName));
                }
                else
                {
                    commonValues.Add(pair);
                }

                pair = buffer.Peek();
            }

            DxfTable result;
            switch (tableType)
            {
                case DxfTable.AppIdText:
                    result = DxfAppIdTable.ReadFromBuffer(buffer);
                    break;
                case DxfTable.BlockRecordText:
                    result = DxfBlockRecordTable.ReadFromBuffer(buffer);
                    break;
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
                case DxfTable.UcsText:
                    result = DxfUcsTable.ReadFromBuffer(buffer);
                    break;
                case DxfTable.ViewText:
                    result = DxfViewTable.ReadFromBuffer(buffer);
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
                            result.Handle = DxfCommonConverters.UIntHandle(common.StringValue);
                            break;
                        case 70:
                            // entry count, read dynamically
                            break;
                        case 330:
                            result.OwnerHandle = DxfCommonConverters.UIntHandle(common.StringValue);
                            break;
                    }
                }

                result.ExtensionDataGroups.AddRange(groups);
                result.AfterRead();
            }

            return result;
        }

        protected virtual void BeforeWrite()
        {
        }

        protected virtual void AfterRead()
        {
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

    public partial class DxfAppIdTable
    {
        protected override void Initialize()
        {
            Items.Add(new DxfAppId() { Name = "ACAD" });
            Items.Add(new DxfAppId() { Name = "ACADANNOTATIVE" });
            Items.Add(new DxfAppId() { Name = "ACAD_NAV_VCDISPLAY" });
            Items.Add(new DxfAppId() { Name = "ACAD_MLEADERVER" });
        }
    }

    public partial class DxfBlockRecordTable
    {
        protected override void BeforeWrite()
        {
            foreach (var blockRecord in Items.Where(item => item != null))
            {
                blockRecord.BeforeWrite();
            }
        }

        protected override void AfterRead()
        {
            foreach (var blockRecord in Items.Where(item => item != null))
            {
                blockRecord.AfterRead();
            }
        }
    }

    public partial class DxfDimStyleTable
    {
        protected override void Initialize()
        {
            Items.Add(new DxfDimStyle() { Name = "STANDARD" });
            Items.Add(new DxfDimStyle() { Name = "ANNOTATIVE" });
        }
    }

    public partial class DxfLayerTable
    {
        protected override void Initialize()
        {
            Items.Add(new DxfLayer("0"));
        }
    }

    public partial class DxfLTypeTable
    {
        protected override void Initialize()
        {
            Items.Add(new DxfLineType() { Name = "CONTINUOUS", Description = "Solid line" });
        }
    }

    public partial class DxfStyleTable
    {
        protected override void Initialize()
        {
            Items.Add(new DxfStyle() { Name = "STANDARD" });
            Items.Add(new DxfStyle() { Name = "ANNOTATIVE" });
        }
    }

    public partial class DxfViewPortTable
    {
        protected override void Initialize()
        {
            Items.Add(new DxfViewPort() { Name = DxfViewPort.ActiveViewPortName });
        }
    }
}
