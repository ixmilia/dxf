using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Dxf.Objects
{
    public enum DxfFlowDirection
    {
        Down = 0,
        Up = 1
    }

    public class DxfTableCellStyle
    {
        public string Name { get; set; }
        public double TextHeight { get; set; }
        public short CellAlignment { get; set; }
        public DxfColor TextColor { get; set; } = DxfColor.ByBlock;
        public DxfColor CellFillColor { get; set; } = DxfColor.FromRawValue(7);
        public bool IsBackgroundColorEnabled { get; set; }
        public int CellDataType { get; set; }
        public int CellUnitType { get; set; }
        public short BorderLineweight1 { get; set; }
        public short BorderLineweight2 { get; set; }
        public short BorderLineweight3 { get; set; }
        public short BorderLineweight4 { get; set; }
        public short BorderLineweight5 { get; set; }
        public short BorderLineweight6 { get; set; }
        public bool IsBorder1Visible { get; set; } = true;
        public bool IsBorder2Visible { get; set; } = true;
        public bool IsBorder3Visible { get; set; } = true;
        public bool IsBorder4Visible { get; set; } = true;
        public bool IsBorder5Visible { get; set; } = true;
        public bool IsBorder6Visible { get; set; } = true;
        public DxfColor Border1Color { get; set; } = DxfColor.ByBlock;
        public DxfColor Border2Color { get; set; } = DxfColor.ByBlock;
        public DxfColor Border3Color { get; set; } = DxfColor.ByBlock;
        public DxfColor Border4Color { get; set; } = DxfColor.ByBlock;
        public DxfColor Border5Color { get; set; } = DxfColor.ByBlock;
        public DxfColor Border6Color { get; set; } = DxfColor.ByBlock;

        internal static DxfTableCellStyle FromBuffer(DxfCodePairBufferReader buffer)
        {
            var seenName = false;
            var style = new DxfTableCellStyle();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                switch (pair.Code)
                {
                    case 7:
                        if (seenName)
                        {
                            // found another cell style; return without consuming
                            return style;
                        }
                        else
                        {
                            style.Name = pair.StringValue;
                            seenName = true;
                        }
                        break;
                    case 62:
                        style.TextColor = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 63:
                        style.CellFillColor = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 64:
                        style.Border1Color = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 65:
                        style.Border2Color = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 66:
                        style.Border3Color = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 67:
                        style.Border4Color = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 68:
                        style.Border5Color = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 69:
                        style.Border6Color = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 90:
                        style.CellDataType = pair.IntegerValue;
                        break;
                    case 91:
                        style.CellUnitType = pair.IntegerValue;
                        break;
                    case 140:
                        style.TextHeight = pair.DoubleValue;
                        break;
                    case 170:
                        style.CellAlignment = pair.ShortValue;
                        break;
                    case 274:
                        style.BorderLineweight1 = pair.ShortValue;
                        break;
                    case 275:
                        style.BorderLineweight2 = pair.ShortValue;
                        break;
                    case 276:
                        style.BorderLineweight3 = pair.ShortValue;
                        break;
                    case 277:
                        style.BorderLineweight4 = pair.ShortValue;
                        break;
                    case 278:
                        style.BorderLineweight5 = pair.ShortValue;
                        break;
                    case 279:
                        style.BorderLineweight6 = pair.ShortValue;
                        break;
                    case 283:
                        style.IsBackgroundColorEnabled = DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                    case 284:
                        style.IsBorder1Visible = DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                    case 285:
                        style.IsBorder2Visible = DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                    case 286:
                        style.IsBorder3Visible = DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                    case 287:
                        style.IsBorder4Visible = DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                    case 288:
                        style.IsBorder5Visible = DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                    case 289:
                        style.IsBorder6Visible = DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                    default:
                        // unknown code, return without consuming the pair
                        return style;
                }

                buffer.Advance();
            }

            return style;
        }
    }

    public partial class DxfTableStyle
    {
        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            int code_280_index = 0;
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                while (this.TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
                }

                switch (pair.Code)
                {
                    case 3:
                        this.Description = (pair.StringValue);
                        buffer.Advance();
                        break;
                    case 7:
                        var style = DxfTableCellStyle.FromBuffer(buffer);
                        CellStyles.Add(style);
                        break;
                    case 40:
                        this.HorizontalCellMargin = (pair.DoubleValue);
                        buffer.Advance();
                        break;
                    case 41:
                        this.VerticalCellMargin = (pair.DoubleValue);
                        buffer.Advance();
                        break;
                    case 70:
                        this.FlowDirection = (DxfFlowDirection)(pair.ShortValue);
                        buffer.Advance();
                        break;
                    case 71:
                        this.Flags = (pair.ShortValue);
                        buffer.Advance();
                        break;
                    case 280:
                        switch (code_280_index)
                        {
                            case 0:
                                this.Version = (DxfVersion)(pair.ShortValue);
                                code_280_index++;
                                break;
                            case 1:
                                this.IsTitleSuppressed = BoolShort(pair.ShortValue);
                                code_280_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 280");
                                break;
                        }

                        buffer.Advance();
                        break;
                    case 281:
                        this.IsColumnHeadingSuppressed = BoolShort(pair.ShortValue);
                        buffer.Advance();
                        break;
                    default:
                        if (!base.TrySetPair(pair))
                        {
                            ExcessCodePairs.Add(pair);
                        }

                        buffer.Advance();
                        break;
                }
            }

            return PostParse();
        }
    }
}
