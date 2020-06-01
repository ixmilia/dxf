using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Blocks
{
    public partial class DxfBlock
    {
        private class DxfEndBlock : IDxfItemInternal
        {
            #region IDxfItem and IDxfItemInternal
            uint IDxfItemInternal.Handle { get; set; }
            uint IDxfItemInternal.OwnerHandle { get; set; }
            public IDxfItem Owner { get; private set; }

            void IDxfItemInternal.SetOwner(IDxfItem owner)
            {
                Owner = owner;
            }

            IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()
            {
                yield break;
            }
            IEnumerable<IDxfItemInternal> IDxfItemInternal.GetChildItems()
            {
                return ((IDxfItemInternal)this).GetPointers().Select(p => (IDxfItemInternal)p.Item);
            }
            #endregion

            public DxfBlock Parent => (DxfBlock)Owner;
            public IList<DxfCodePairGroup> ExtensionDataGroups { get; }

            public DxfEndBlock(DxfBlock parent)
            {
                Owner = parent;
                ExtensionDataGroups = new ListNonNull<DxfCodePairGroup>();
            }

            public void ApplyCodePairs(DxfCodePairBufferReader buffer)
            {
                var pair = buffer.Peek();
                buffer.Advance();
                switch (pair.Code)
                {
                    case 5:
                        ((IDxfItemInternal)this).Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                        break;
                    case 8:
                        // just a re-iteration of the layer
                        break;
                    case 67:
                        // just a re-iteration of the paper space setting
                        break;
                    case 100:
                        Debug.Assert(pair.StringValue == AcDbEntityText || pair.StringValue == AcDbBlockEndText);
                        break;
                    case DxfCodePairGroup.GroupCodeNumber:
                        var groupName = DxfCodePairGroup.GetGroupName(pair.StringValue);
                        ExtensionDataGroups.Add(DxfCodePairGroup.FromBuffer(buffer, groupName));
                        break;
                }
            }

            public IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version, bool outputHandles)
            {
                var list = new List<DxfCodePair>();
                list.Add(new DxfCodePair(0, EndBlockText));
                if (outputHandles && ((IDxfItemInternal)this).Handle != 0u)
                {
                    list.Add(new DxfCodePair(5, DxfCommonConverters.UIntHandle(((IDxfItemInternal)this).Handle)));
                }

                if (Parent.XData != null)
                {
                    Parent.XData.AddValuePairs(list, version, outputHandles);
                }

                if (version >= DxfAcadVersion.R14)
                {
                    foreach (var group in ExtensionDataGroups)
                    {
                        group.AddValuePairs(list, version, outputHandles);
                    }
                }

                if (version >= DxfAcadVersion.R2000)
                {
                    list.Add(new DxfCodePair(330, DxfCommonConverters.UIntHandle(((IDxfItemInternal)Parent).OwnerHandle)));
                }

                if (version >= DxfAcadVersion.R13)
                {
                    list.Add(new DxfCodePair(100, AcDbEntityText));
                }

                if (Parent.IsInPaperSpace)
                {
                    list.Add(new DxfCodePair(67, DxfCommonConverters.BoolShort(Parent.IsInPaperSpace)));
                }

                list.Add(new DxfCodePair(8, Parent.Layer));

                if (version >= DxfAcadVersion.R13)
                {
                    list.Add(new DxfCodePair(100, AcDbBlockEndText));
                }
                return list;
            }
        }
    }
}
