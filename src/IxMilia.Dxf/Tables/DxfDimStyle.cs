using System;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf
{
    public partial class DxfDimStyle
    {
        public const string XDataStyleName = "DSTYLE";

        public bool TryGetStyleFromXDataDifference(DxfXDataApplicationItemCollection xdataItemCollection, out DxfDimStyle style)
        {
            // style data is encoded as
            //   1001 DSTYLE
            //   1002 {
            //     ... style overrides
            //   1002 }

            style = default(DxfDimStyle);
            if (xdataItemCollection == null)
            {
                return false;
            }

            for (int i = 0; i < xdataItemCollection.Count - 1; i++)
            {
                if (xdataItemCollection[i] is DxfXDataString xdataString && xdataString.Value == XDataStyleName &&
                    xdataItemCollection[i + 1] is DxfXDataItemList itemList)
                {
                    if (itemList.Items.Count % 2 != 0)
                    {
                        // must be an even number
                        return false;
                    }

                    var codePairs = new List<DxfCodePair>();
                    for (int j = 0; j < itemList.Items.Count; j += 2)
                    {
                        if (!(itemList.Items[j] is DxfXDataInteger codeItem))
                        {
                            // must alternate between int/<data>
                            return false;
                        }

                        DxfCodePair pair;
                        var valueItem = itemList.Items[j + 1];
                        var code = codeItem.Value;
                        switch (DxfCodePair.ExpectedType(code).Name)
                        {
                            case nameof(Boolean):
                                pair = new DxfCodePair(code, true);
                                break;
                            case nameof(Double) when valueItem is DxfXDataDistance dist:
                                pair = new DxfCodePair(code, dist.Value);
                                break;
                            case nameof(Double) when valueItem is DxfXDataReal real:
                                pair = new DxfCodePair(code, real.Value);
                                break;
                            case nameof(Double) when valueItem is DxfXDataScaleFactor scale:
                                pair = new DxfCodePair(code, scale.Value);
                                break;
                            case nameof(Int16) when valueItem is DxfXDataInteger i32:
                                pair = new DxfCodePair(code, i32.Value);
                                break;
                            case nameof(Int32) when valueItem is DxfXDataLong i32:
                                pair = new DxfCodePair(code, i32.Value);
                                break;
                            case nameof(Int64) when valueItem is DxfXDataLong i32:
                                pair = new DxfCodePair(code, i32.Value);
                                break;
                            case nameof(String) when valueItem is DxfXDataString str:
                                pair = new DxfCodePair(code, str.Value);
                                break;
                            default:
                                // unexpected code pair type
                                return false;
                        }

                        codePairs.Add(pair);
                    }

                    if (codePairs.Count == 0)
                    {
                        // no difference to apply
                        return false;
                    }

                    // if we made it this far, there is a difference that should be applied
                    style = Clone();
                    foreach (var pair in codePairs)
                    {
                        style.ApplyCodePair(pair);
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
