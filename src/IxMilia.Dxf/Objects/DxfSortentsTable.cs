// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Objects
{
    public partial class DxfSortentsTable
    {
        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            var isReadyForSortHandles = false;
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                if (TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
                }

                switch (pair.Code)
                {
                    case 5:
                        if (isReadyForSortHandles)
                        {
                            SortHandles.Add(DxfCommonConverters.UIntHandle(pair.StringValue));
                        }
                        else
                        {
                            Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                            isReadyForSortHandles = true;
                        }
                        break;
                    case 100:
                        isReadyForSortHandles = true;
                        break;
                    case 330:
                        OwnerHandle = DxfCommonConverters.UIntHandle(pair.StringValue);
                        isReadyForSortHandles = true;
                        break;
                    case 331:
                        EntityHandles.Add(DxfCommonConverters.UIntHandle(pair.StringValue));
                        isReadyForSortHandles = true;
                        break;
                    default:
                        ExcessCodePairs.Add(pair);
                        break;
                }

                buffer.Advance();
            }

            return PostParse();
        }
    }
}
