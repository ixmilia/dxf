namespace IxMilia.Dxf.Entities
{
    public partial class DxfMText
    {
        private bool _readingColumnData = false;
        private bool _readColumnCount = false;

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 1:
                    this.Text = (pair.StringValue);
                    break;
                case 3:
                    ExtendedText.Add(pair.StringValue);
                    break;
                case 7:
                    this.TextStyleName = (pair.StringValue);
                    break;
                case 10:
                    this.InsertionPoint = this.InsertionPoint.WithUpdatedX(pair.DoubleValue);
                    break;
                case 20:
                    this.InsertionPoint = this.InsertionPoint.WithUpdatedY(pair.DoubleValue);
                    break;
                case 30:
                    this.InsertionPoint = this.InsertionPoint.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 11:
                    this.XAxisDirection = this.XAxisDirection.WithUpdatedX(pair.DoubleValue);
                    break;
                case 21:
                    this.XAxisDirection = this.XAxisDirection.WithUpdatedY(pair.DoubleValue);
                    break;
                case 31:
                    this.XAxisDirection = this.XAxisDirection.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 40:
                    this.InitialTextHeight = (pair.DoubleValue);
                    break;
                case 41:
                    this.ReferenceRectangleWidth = (pair.DoubleValue);
                    break;
                case 42:
                    this.HorizontalWidth = (pair.DoubleValue);
                    break;
                case 43:
                    this.VerticalHeight = (pair.DoubleValue);
                    break;
                case 44:
                    this.LineSpacingFactor = (pair.DoubleValue);
                    break;
                case 45:
                    this.FillBoxScale = (pair.DoubleValue);
                    break;
                case 48:
                    this.ColumnWidth = (pair.DoubleValue);
                    break;
                case 49:
                    this.ColumnGutter = (pair.DoubleValue);
                    break;
                case 50:
                    if (_readingColumnData)
                    {
                        if (_readColumnCount)
                        {
                            ColumnHeights.Add(pair.DoubleValue);
                        }
                        else
                        {
                            var columnCount = (int)pair.DoubleValue;
                            _readColumnCount = true;
                        }
                    }
                    else
                    {
                        RotationAngle = pair.DoubleValue;
                    }

                    break;
                case 63:
                    this.BackgroundFillColor = DxfColor.FromRawValue(pair.ShortValue);
                    break;
                case 71:
                    this.AttachmentPoint = (DxfAttachmentPoint)(pair.ShortValue);
                    break;
                case 72:
                    this.DrawingDirection = (DxfDrawingDirection)(pair.ShortValue);
                    break;
                case 73:
                    this.LineSpacingStyle = (DxfMTextLineSpacingStyle)(pair.ShortValue);
                    break;
                case 75:
                    this.ColumnType = (pair.ShortValue);
                    _readingColumnData = true;
                    break;
                case 76:
                    this.ColumnCount = (int)(pair.ShortValue);
                    break;
                case 78:
                    this.IsColumnFlowReversed = BoolShort(pair.ShortValue);
                    break;
                case 79:
                    this.IsColumnAutoHeight = BoolShort(pair.ShortValue);
                    break;
                case 90:
                    this.BackgroundFillSetting = (DxfBackgroundFillSetting)(pair.IntegerValue);
                    break;
                case 210:
                    this.ExtrusionDirection = this.ExtrusionDirection.WithUpdatedX(pair.DoubleValue);
                    break;
                case 220:
                    this.ExtrusionDirection = this.ExtrusionDirection.WithUpdatedY(pair.DoubleValue);
                    break;
                case 230:
                    this.ExtrusionDirection = this.ExtrusionDirection.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 420:
                case 421:
                case 422:
                case 423:
                case 424:
                case 425:
                case 426:
                case 427:
                case 428:
                case 429:
                    this.BackgroundColorRGB = (pair.IntegerValue);
                    break;
                case 430:
                case 431:
                case 432:
                case 433:
                case 434:
                case 435:
                case 436:
                case 437:
                case 438:
                case 439:
                    this.BackgroundColorName = (pair.StringValue);
                    break;
                case 441:
                    this.BackgroundFillColorTransparency = (pair.IntegerValue);
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }
    }
}
