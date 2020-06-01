using System;

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
        public string LayoutName
        {
            get => _layoutName;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException($"{nameof(LayoutName)} must be non-empty.");
                }

                _layoutName = value;
            }
        }

        public override string PlotViewName
        {
            get => string.IsNullOrEmpty(base.PlotViewName) ? LayoutName : base.PlotViewName;
            set => base.PlotViewName = value;
        }

        public DxfLayout(string plotViewName, string layoutName)
            : base(plotViewName)
        {
            LayoutName = layoutName;
        }

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
                    this._layoutName = pair.StringValue;
                    break;
                case 10:
                    this.MinimumLimits = this.MinimumLimits.WithUpdatedX(pair.DoubleValue);
                    break;
                case 20:
                    this.MinimumLimits = this.MinimumLimits.WithUpdatedY(pair.DoubleValue);
                    break;
                case 11:
                    this.MaximumLimits = this.MaximumLimits.WithUpdatedX(pair.DoubleValue);
                    break;
                case 21:
                    this.MaximumLimits = this.MaximumLimits.WithUpdatedY(pair.DoubleValue);
                    break;
                case 12:
                    this.InsertionBasePoint = this.InsertionBasePoint.WithUpdatedX(pair.DoubleValue);
                    break;
                case 22:
                    this.InsertionBasePoint = this.InsertionBasePoint.WithUpdatedY(pair.DoubleValue);
                    break;
                case 32:
                    this.InsertionBasePoint = this.InsertionBasePoint.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 13:
                    this.UcsOrigin = this.UcsOrigin.WithUpdatedX(pair.DoubleValue);
                    break;
                case 23:
                    this.UcsOrigin = this.UcsOrigin.WithUpdatedY(pair.DoubleValue);
                    break;
                case 33:
                    this.UcsOrigin = this.UcsOrigin.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 14:
                    this.MinimumExtents = this.MinimumExtents.WithUpdatedX(pair.DoubleValue);
                    break;
                case 24:
                    this.MinimumExtents = this.MinimumExtents.WithUpdatedY(pair.DoubleValue);
                    break;
                case 34:
                    this.MinimumExtents = this.MinimumExtents.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 15:
                    this.MaximumExtents = this.MaximumExtents.WithUpdatedX(pair.DoubleValue);
                    break;
                case 25:
                    this.MaximumExtents = this.MaximumExtents.WithUpdatedY(pair.DoubleValue);
                    break;
                case 35:
                    this.MaximumExtents = this.MaximumExtents.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 16:
                    this.UcsXAxis = this.UcsXAxis.WithUpdatedX(pair.DoubleValue);
                    break;
                case 26:
                    this.UcsXAxis = this.UcsXAxis.WithUpdatedY(pair.DoubleValue);
                    break;
                case 36:
                    this.UcsXAxis = this.UcsXAxis.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 17:
                    this.UcsYAxis = this.UcsYAxis.WithUpdatedX(pair.DoubleValue);
                    break;
                case 27:
                    this.UcsYAxis = this.UcsYAxis.WithUpdatedY(pair.DoubleValue);
                    break;
                case 37:
                    this.UcsYAxis = this.UcsYAxis.WithUpdatedZ(pair.DoubleValue);
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
                    this.ViewportPointer.Handle = UIntHandle(pair.StringValue);
                    break;
                case 333:
                    this.ShadePlotObjectPointer.Handle = UIntHandle(pair.StringValue);
                    break;
                case 345:
                    this.TableRecordPointer.Handle = UIntHandle(pair.StringValue);
                    break;
                case 346:
                    this.TableRecordBasePointer.Handle = UIntHandle(pair.StringValue);
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }
    }
}
