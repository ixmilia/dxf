// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfXRecordObject
    {
        public List<DxfCodePair> DataPairs { get; } = new List<DxfCodePair>();

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            base.AddValuePairs(pairs, version, outputHandles);
            pairs.Add(new DxfCodePair(100, "AcDbXrecord"));
            pairs.Add(new DxfCodePair(280, (short)(this.DuplicateRecordHandling)));
            pairs.AddRange(DataPairs);
        }

        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            bool readDuplicateFlag = false;
            bool readingData = false;
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                if (readingData)
                {
                    DataPairs.Add(pair);
                }
                else
                {
                    if (base.TrySetPair(pair))
                    {
                        buffer.Advance();
                        continue;
                    }

                    if (this.TrySetExtensionData(pair, buffer))
                    {
                        continue;
                    }

                    if (pair.Code == 100)
                    {
                        Debug.Assert(pair.StringValue == "AcDbXrecord");
                        buffer.Advance();
                        continue;
                    }

                    if (pair.Code == 280 && !readDuplicateFlag)
                    {
                        DuplicateRecordHandling = (DxfDictionaryDuplicateRecordHandling)pair.ShortValue;
                        readDuplicateFlag = true;
                        readingData = true;
                    }
                    else if (pair.Code == 5 || pair.Code == 105)
                    {
                        // these codes aren't allowed here
                        ExcessCodePairs.Add(pair);
                    }
                    else
                    {
                        DataPairs.Add(pair);
                    }
                }

                buffer.Advance();
            }

            return PostParse();
        }
    }
}
