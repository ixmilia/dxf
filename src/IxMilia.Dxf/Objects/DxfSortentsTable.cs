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

                while (this.TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
                }

                switch (pair.Code)
                {
                    case 5:
                        if (isReadyForSortHandles)
                        {
                            SortItemsPointers.Pointers.Add(new DxfPointer(DxfCommonConverters.UIntHandle(pair.StringValue)));
                        }
                        else
                        {
                            ((IDxfItemInternal)this).Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                            isReadyForSortHandles = true;
                        }
                        break;
                    case 100:
                        isReadyForSortHandles = true;
                        break;
                    case 330:
                        ((IDxfItemInternal)this).OwnerHandle = DxfCommonConverters.UIntHandle(pair.StringValue);
                        isReadyForSortHandles = true;
                        break;
                    case 331:
                        EntitiesPointers.Pointers.Add(new DxfPointer(DxfCommonConverters.UIntHandle(pair.StringValue)));
                        isReadyForSortHandles = true;
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
