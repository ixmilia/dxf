#nullable enable

using System.Diagnostics;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfSpatialIndex
    {
        private string LastSubclassMarker = string.Empty;

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 100:
                    LastSubclassMarker = pair.StringValue;
                    break;
                case 40:
                    switch (LastSubclassMarker)
                    {
                        case "":
                        case "AcDbIndex":
                            this.Timestamp = DateDouble(pair.DoubleValue);
                            break;
                        case "AcDbSpatialIndex":
                            this.Values.Add(pair.DoubleValue);
                            break;
                        default:
                            Debug.Assert(false, "Unexpected extra values for code 40");
                            break;
                    }
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }
    }
}
