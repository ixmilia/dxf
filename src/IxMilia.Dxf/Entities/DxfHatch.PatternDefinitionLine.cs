using System.Collections.Generic;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfHatch
    {
        public class PatternDefinitionLine
        {
            public double Angle { get; set; } = 0.0;
            public DxfPoint BasePoint { get; set; } = DxfPoint.Origin;
            public DxfVector Offset { get; set; } = DxfVector.Zero;
            public List<double> DashLengths { get; } = new List<double>();

            internal bool TrySetPair(DxfCodePair pair)
            {
                switch (pair.Code)
                {
                    case 53:
                        Angle = pair.DoubleValue;
                        break;
                    case 43:
                        BasePoint = BasePoint.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 44:
                        BasePoint = BasePoint.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 45:
                        Offset = Offset.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 46:
                        Offset = Offset.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 79:
                        var _dashLengthCount = pair.ShortValue;
                        break;
                    case 49:
                        DashLengths.Add(pair.DoubleValue);
                        break;
                    default:
                        return false;
                }

                return true;
            }
        }
    }
}
