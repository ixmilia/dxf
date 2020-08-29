using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfXRecordObject
    {
        public IList<DxfCodePair> DataPairs { get; } = new ListNonNull<DxfCodePair>();

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, bool writeXData)
        {
            base.AddValuePairs(pairs, version, outputHandles, writeXData: false);
            pairs.Add(new DxfCodePair(100, "AcDbXrecord"));
            pairs.Add(new DxfCodePair(280, (short)(this.DuplicateRecordHandling)));
            pairs.AddRange(DataPairs);

            if (writeXData)
            {
                DxfXData.AddValuePairs(XData, pairs, version, outputHandles);
            }
        }

        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
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
                    if (pair.Code == 280)
                    {
                        DuplicateRecordHandling = (DxfDictionaryDuplicateRecordHandling)pair.ShortValue;
                        buffer.Advance();
                        readingData = true;
                        continue;
                    }

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
                    else if (pair.Code == 5 || pair.Code == 105)
                    {
                        // these codes aren't allowed here
                        ExcessCodePairs.Add(pair);
                    }
                    else
                    {
                        DataPairs.Add(pair);
                        readingData = true;
                    }
                }

                buffer.Advance();
            }

            return PostParse();
        }
    }
}
