// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfLightList
    {
        public class DxfLightListItem
        {
            public uint Handle { get; set; }
            public string Name { get; set; }
        }

        public List<DxfLightListItem> Lights { get; } = new List<DxfLightListItem>();

        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            bool readVersionNumber = false;
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
                    case 5:
                        // start a new light item
                        Lights.Add(new DxfLightListItem());
                        Lights.Last().Handle = UIntHandle(pair.StringValue);
                        break;
                    case 1:
                        Lights.Last().Name = pair.StringValue;
                        break;
                    case 90:
                        if (readVersionNumber)
                        {
                            // count of lights is ignored since it's implicitly set by reading the values
                        }
                        else
                        {
                            Version = pair.IntegerValue;
                            readVersionNumber = true;
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

            return PostParse();
        }
    }
}
