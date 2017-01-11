// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            bool readElementCount = false;
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
                    case 2:
                        this.StyleName = (pair.StringValue);
                        break;
                    case 3:
                        this.Description = (pair.StringValue);
                        break;
                    case 6:
                        this._elementLineTypes.Add((pair.StringValue));
                        break;
                    case 49:
                        this._elementOffsets.Add((pair.DoubleValue));
                        break;
                    case 51:
                        this.StartAngle = (pair.DoubleValue);
                        break;
                    case 52:
                        this.EndAngle = (pair.DoubleValue);
                        break;
                    case 62:
                        if (readElementCount)
                        {
                            this._elementColors.Add(FromRawValue(pair.ShortValue));
                        }
                        else
                        {
                            this.FillColor = FromRawValue(pair.ShortValue);
                        }
                        break;
                    case 70:
                        this._flags = (pair.ShortValue);
                        break;
                    case 71:
                        this._elementCount = (pair.ShortValue);
                        readElementCount = true;
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

            return PostParse();
        }

        protected override DxfObject PostParse()
        {
            Debug.Assert(_elementCount == _elementOffsets.Count);
            Debug.Assert(_elementCount == _elementColors.Count);
            Debug.Assert(_elementCount == _elementLineTypes.Count);
            for (int i = 0; i < _elementCount; i++)
            {
                Elements.Add(new DxfMLineStyleElement() { Offset = _elementOffsets[i], Color = _elementColors[i], LineType = _elementLineTypes[i] });
            }

            _elementOffsets.Clear();
            _elementColors.Clear();
            _elementLineTypes.Clear();

            return this;
        }
    }
}
