// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace IxMilia.Dxf.Objects
{
    public enum DxfUcsOrthographicType
    {
        NotOrthographic = 0,
        Top = 1,
        Bottom = 2,
        Front = 3,
        Back = 4,
        Left = 5,
        Right = 6
    }

    public partial class DxfLayout
    {
        public IDxfItem PaperSpaceObject
        {
            get { return Owner; }
            set { ((IDxfItemInternal)this).SetOwner(value); }
        }

        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            bool isReadingPlotSettings = true;
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

                if (isReadingPlotSettings)
                {
                    if (pair.Code == 100 && pair.StringValue == "AcDbLayout")
                    {
                        isReadingPlotSettings = false;
                    }
                    else
                    {
                        if (!base.TrySetPair(pair))
                        {
                            ExcessCodePairs.Add(pair);
                        }
                    }
                }
                else
                {
                    if (!TrySetPair(pair))
                    {
                        ExcessCodePairs.Add(pair);
                    }
                }

                buffer.Advance();
            }

            return PostParse();
        }

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 1:
                    this.LayoutName = (pair.StringValue);
                    break;
                case 10:
                    this.MinimumLimits.X = pair.DoubleValue;
                    break;
                case 20:
                    this.MinimumLimits.Y = pair.DoubleValue;
                    break;
                case 11:
                    this.MaximumLimits.X = pair.DoubleValue;
                    break;
                case 21:
                    this.MaximumLimits.Y = pair.DoubleValue;
                    break;
                case 12:
                    this.InsertionBasePoint.X = pair.DoubleValue;
                    break;
                case 22:
                    this.InsertionBasePoint.Y = pair.DoubleValue;
                    break;
                case 32:
                    this.InsertionBasePoint.Z = pair.DoubleValue;
                    break;
                case 13:
                    this.UcsOrigin.X = pair.DoubleValue;
                    break;
                case 23:
                    this.UcsOrigin.Y = pair.DoubleValue;
                    break;
                case 33:
                    this.UcsOrigin.Z = pair.DoubleValue;
                    break;
                case 14:
                    this.MinimumExtents.X = pair.DoubleValue;
                    break;
                case 24:
                    this.MinimumExtents.Y = pair.DoubleValue;
                    break;
                case 34:
                    this.MinimumExtents.Z = pair.DoubleValue;
                    break;
                case 15:
                    this.MaximumExtents.X = pair.DoubleValue;
                    break;
                case 25:
                    this.MaximumExtents.Y = pair.DoubleValue;
                    break;
                case 35:
                    this.MaximumExtents.Z = pair.DoubleValue;
                    break;
                case 16:
                    this.UcsXAxis.X = pair.DoubleValue;
                    break;
                case 26:
                    this.UcsXAxis.Y = pair.DoubleValue;
                    break;
                case 36:
                    this.UcsXAxis.Z = pair.DoubleValue;
                    break;
                case 17:
                    this.UcsYAxis.X = pair.DoubleValue;
                    break;
                case 27:
                    this.UcsYAxis.Y = pair.DoubleValue;
                    break;
                case 37:
                    this.UcsYAxis.Z = pair.DoubleValue;
                    break;
                case 70:
                    this.LayoutFlags = (pair.ShortValue);
                    break;
                case 71:
                    this.TabOrder = (pair.ShortValue);
                    break;
                case 76:
                    this.UcsOrthographicType = (DxfUcsOrthographicType)(pair.ShortValue);
                    break;
                case 146:
                    this.Elevation = (pair.DoubleValue);
                    break;
                case 331:
                    this.ViewportHandle = UIntHandle(pair.StringValue);
                    break;
                case 333:
                    this.ShadePlotHandle = UIntHandle(pair.StringValue);
                    break;
                case 345:
                    this.TableRecordHandle = UIntHandle(pair.StringValue);
                    break;
                case 346:
                    this.TableRecordBaseHandle = UIntHandle(pair.StringValue);
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }
    }
}
