using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfMLineStyle
    {
        public class DxfMLineStyleElement
        {
            public double Offset { get; set; }
            public DxfColor Color { get; set; }
            public string LineType { get; set; }
        }

        public IList<DxfMLineStyleElement> Elements { get; } = new ListNonNull<DxfMLineStyleElement>();

        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            var readElementCount = false;
            var elementCount = 0;
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

                if (pair.Code == 0)
                {
                    break;
                }

                switch (pair.Code)
                {
                    case 2:
                        this.StyleName = (pair.StringValue);
                        break;
                    case 3:
                        this.Description = (pair.StringValue);
                        break;
                    case 51:
                        this.StartAngle = (pair.DoubleValue);
                        break;
                    case 52:
                        this.EndAngle = (pair.DoubleValue);
                        break;
                    case 70:
                        this._flags = (pair.ShortValue);
                        break;
                    case 71:
                        elementCount = (pair.ShortValue);
                        readElementCount = true;
                        break;
                    case 49:
                        // found a new element value
                        Elements.Add(new DxfMLineStyleElement() { Offset = pair.DoubleValue });
                        break;
                    case 62:
                        if (readElementCount)
                        {
                            // update the last element
                            if (Elements.Count > 0)
                            {
                                Elements[Elements.Count - 1].Color = FromRawValue(pair.ShortValue);
                            }
                        }
                        else
                        {
                            this.FillColor = FromRawValue(pair.ShortValue);
                        }
                        break;
                    case 6:
                        // update the last element
                        if (Elements.Count > 0)
                        {
                            Elements[Elements.Count - 1].LineType = pair.StringValue;
                        }
                        break;
                    default:
                        if (!base.TrySetPair(pair))
                        {
                            ExcessCodePairs.Add(pair);
                        }
                        break;
                }

                buffer.Advance();
            }

            return this;
        }
    }
}
