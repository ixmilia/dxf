// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using IxMilia.Dxf.Tables;

namespace IxMilia.Dxf.Sections
{
    internal class DxfTablesSection : DxfSection
    {
        public override DxfSectionType Type
        {
            get { return DxfSectionType.Tables; }
        }

        public DxfAppIdTable AppIdTable { get; private set; }
        public DxfBlockRecordTable BlockRecordTable { get; private set; }
        public DxfDimStyleTable DimStyleTable { get; private set; }
        public DxfLayerTable LayerTable { get; private set; }
        public DxfLTypeTable LTypeTable { get; private set; }
        public DxfStyleTable StyleTable { get; private set; }
        public DxfUcsTable UcsTable { get; private set; }
        public DxfViewTable ViewTable { get; private set; }
        public DxfViewPortTable ViewPortTable { get; private set; }

        public DxfTablesSection()
        {
            this.AppIdTable = new DxfAppIdTable();
            this.BlockRecordTable = new DxfBlockRecordTable();
            this.DimStyleTable = new DxfDimStyleTable();
            this.LayerTable = new DxfLayerTable();
            this.LTypeTable = new DxfLTypeTable();
            this.StyleTable = new DxfStyleTable();
            this.UcsTable = new DxfUcsTable();
            this.ViewTable = new DxfViewTable();
            this.ViewPortTable = new DxfViewPortTable();
        }

        internal IEnumerable<DxfTable> GetTables(DxfAcadVersion version)
        {
            yield return ViewPortTable;
            yield return LTypeTable;
            yield return LayerTable;
            yield return StyleTable;
            yield return ViewTable;
            yield return UcsTable;

            if (version >= DxfAcadVersion.R12)
            {
                yield return AppIdTable;
            }

            if (version >= DxfAcadVersion.R12)
            {
                yield return DimStyleTable;
            }

            if (version >= DxfAcadVersion.R13)
            {
                yield return BlockRecordTable;
            }
        }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
            foreach (var table in GetTables(version))
            {
                if (writtenItems.Add(table))
                {
                    foreach (var pair in table.GetValuePairs(version, outputHandles, writtenItems))
                    {
                        yield return pair;
                    }
                }
            }
        }

        protected internal override void Clear()
        {
            AppIdTable.Items.Clear();
            BlockRecordTable.Items.Clear();
            DimStyleTable.Items.Clear();
            LayerTable.Items.Clear();
            LTypeTable.Items.Clear();
            StyleTable.Items.Clear();
            UcsTable.Items.Clear();
            ViewPortTable.Items.Clear();
            ViewTable.Items.Clear();
        }

        internal static DxfTablesSection TablesSectionFromBuffer(DxfCodePairBufferReader buffer)
        {
            var section = new DxfTablesSection();
            section.Clear();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                buffer.Advance();
                if (DxfCodePair.IsSectionEnd(pair))
                {
                    break;
                }

                if (!IsTableStart(pair))
                {
                    throw new DxfReadException("Expected start of table.", pair);
                }

                var table = DxfTable.FromBuffer(buffer);
                if (table != null)
                {
                    switch (table.TableType)
                    {
                        case DxfTableType.AppId:
                            section.AppIdTable = (DxfAppIdTable)table;
                            break;
                        case DxfTableType.BlockRecord:
                            section.BlockRecordTable = (DxfBlockRecordTable)table;
                            break;
                        case DxfTableType.DimStyle:
                            section.DimStyleTable = (DxfDimStyleTable)table;
                            break;
                        case DxfTableType.Layer:
                            section.LayerTable = (DxfLayerTable)table;
                            break;
                        case DxfTableType.LType:
                            section.LTypeTable = (DxfLTypeTable)table;
                            break;
                        case DxfTableType.Style:
                            section.StyleTable = (DxfStyleTable)table;
                            break;
                        case DxfTableType.Ucs:
                            section.UcsTable = (DxfUcsTable)table;
                            break;
                        case DxfTableType.View:
                            section.ViewTable = (DxfViewTable)table;
                            break;
                        case DxfTableType.ViewPort:
                            section.ViewPortTable = (DxfViewPortTable)table;
                            break;
                        default:
                            throw new DxfReadException($"Unexpected table type {table.TableType}", pair);
                    }
                }
            }

            return section;
        }

        internal static bool IsTableStart(DxfCodePair pair)
        {
            return pair.Code == 0 && pair.StringValue == DxfSection.TableText;
        }

        internal static bool IsTableEnd(DxfCodePair pair)
        {
            return pair.Code == 0 && pair.StringValue == DxfSection.EndTableText;
        }
    }
}
