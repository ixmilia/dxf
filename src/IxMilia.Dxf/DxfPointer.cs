using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Extensions;
using IxMilia.Dxf.Objects;

namespace IxMilia.Dxf
{
    internal class DxfPointer
    {
        public DxfHandle Handle { get; set; }
        public IDxfItem? Item { get; set; }

        public DxfPointer()
            : this(default(DxfHandle), null)
        {
        }

        public DxfPointer(DxfHandle handle)
            : this(handle, null)
        {
        }

        public DxfPointer(IDxfItem? item)
            : this(default(DxfHandle), item)
        {
        }

        public DxfPointer(DxfHandle handle, IDxfItem? item)
        {
            Handle = handle;
            Item = item;
        }

        public static void BindPointers(DxfFile file)
        {
            // gather all items by handle
            var handleMap = new Dictionary<DxfHandle, IDxfItemInternal>();
            var visitedItems = new HashSet<IDxfItemInternal>();
            foreach (var item in file.GetFileItems())
            {
                GatherPointers(item, handleMap, visitedItems);
            }

            visitedItems.Clear();
            var visitedChildren = new HashSet<IDxfItemInternal>();

            // bind all pointers
            foreach (var item in file.GetFileItems())
            {
                BindPointers(item, handleMap, visitedItems, visitedChildren);
            }
        }

        private static void GatherPointers(IDxfItemInternal item, Dictionary<DxfHandle, IDxfItemInternal> handleMap, HashSet<IDxfItemInternal> visitedItems)
        {
            if (item != null && visitedItems.Add(item))
            {
                if (item.Handle.HasValue)
                {
                    handleMap[item.Handle] = item;
                }

                foreach (var child in item.GetChildItems())
                {
                    GatherPointers(child, handleMap, visitedItems);
                }
            }
        }

        private static void BindPointers(IDxfItemInternal item, Dictionary<DxfHandle, IDxfItemInternal> handleMap, HashSet<IDxfItemInternal> visitedItems, HashSet<IDxfItemInternal> visitedChildren)
        {
            if (visitedItems.Add(item))
            {
                // set initial owners
                SetChildOwners(item, visitedChildren);

                // set explicit owners
                BindItemPointers(item, handleMap, visitedItems, visitedChildren);
                foreach (var child in item.GetChildItems())
                {
                    if (child == null)
                    {
                        continue;
                    }

                    BindItemPointers(child, handleMap, visitedItems, visitedChildren);
                }
            }
        }

        private static void BindItemPointers(IDxfItemInternal item, Dictionary<DxfHandle, IDxfItemInternal> handleMap, HashSet<IDxfItemInternal> visitedItems, HashSet<IDxfItemInternal> visitedChildren)
        {
            foreach (var child in item.GetPointers())
            {
                if (handleMap.ContainsKey(child.Handle))
                {
                    child.Item = handleMap[child.Handle];
                    BindPointers((IDxfItemInternal)child.Item, handleMap, visitedItems, visitedChildren);
                    SetOwner((IDxfItemInternal)child.Item, item, isWriting: false);
                }
            }
        }

        private static void SetChildOwners(IDxfItemInternal item, HashSet<IDxfItemInternal> visitedChildren)
        {
            foreach (var child in item.GetChildItems())
            {
                if (child != null && visitedChildren.Add(child))
                {
                    SetOwner(child, item, isWriting: false);
                    SetChildOwners(child, visitedChildren);
                }
            }
        }

        private static void SetOwner(IDxfItemInternal item, IDxfItemInternal owner, bool isWriting)
        {
            if (item != null && owner != null)
            {
                if (isWriting && item is DxfEntity && !(owner is DxfDictionary))
                {
                    // entities can only be parented by a dictionary
                    return;
                }

                item.SetOwner(owner);
                var hasXData = item as IDxfHasXData;
                if (hasXData != null)
                {
                    var reactors = hasXData.ExtensionDataGroups.FirstOrDefault(e => e.GroupName == "ACAD_REACTORS");
                    if (reactors != null)
                    {
                        foreach (var pair in reactors.Items)
                        {
                            SetOwnerPair(pair, owner);
                        }
                    }
                }
            }
        }

        private static void SetOwnerPair(IDxfCodePairOrGroup codePairOrGroup, IDxfItemInternal owner)
        {
            if (codePairOrGroup.IsCodePair)
            {
                var pair = (DxfCodePair)codePairOrGroup;
                if (pair.Code == 330)
                {
                    pair.Value = DxfCommonConverters.HandleString(owner.Handle);
                }
            }
            else
            {
                foreach (var item in ((DxfCodePairGroup)codePairOrGroup).Items)
                {
                    SetOwnerPair(item, owner);
                }
            }
        }

        public static DxfHandle AssignHandles(DxfFile file)
        {
            var visitedItems = new HashSet<IDxfItemInternal>();

            // find max handle
            var maxHandle = new DxfHandle(0x00);
            foreach (var item in file.GetFileItems().WhereNotNull())
            {
                maxHandle = new DxfHandle(Math.Max(maxHandle.Value, item.Handle.Value));
                maxHandle = GetMaxHandleFromChildren(maxHandle, item, visitedItems);
            }

            // assign new handles to be greater than max
            visitedItems.Clear();
            var nextPointer = new DxfHandle(maxHandle.Value + 1);
            foreach (var item in file.GetFileItems().WhereNotNull())
            {
                nextPointer = AssignHandles(item, nextPointer, default, visitedItems);
                foreach (var child in item.GetChildItems().WhereNotNull())
                {
                    var parentHandle = GetParentHandle(item, child);
                    nextPointer = AssignHandles(child, nextPointer, parentHandle, visitedItems);
                    SetOwner(child, item, isWriting: true);
                }
            }

            return nextPointer;
        }

        private static DxfHandle GetMaxHandleFromChildren(DxfHandle parentMaxHandle, IDxfItemInternal item, HashSet<IDxfItemInternal> visitedItems)
        {
            var nextMaxHandle = parentMaxHandle;
            foreach (var child in item.GetPointers().Where(c => c.Item != null))
            {
                var childItem = child.Item as IDxfItemInternal;
                if (childItem is not null)
                {
                    if (visitedItems.Add(childItem))
                    {
                        nextMaxHandle = new DxfHandle(Math.Max(nextMaxHandle.Value, childItem.Handle.Value));
                        nextMaxHandle = GetMaxHandleFromChildren(nextMaxHandle, childItem, visitedItems);
                    }
                }
            }

            return nextMaxHandle;
        }

        private static DxfHandle AssignHandles(IDxfItemInternal? item, DxfHandle nextHandle, DxfHandle ownerHandle, HashSet<IDxfItemInternal> visitedItems)
        {
            if (item is null || !visitedItems.Add(item))
            {
                return nextHandle;
            }

            if (!item.Handle.HasValue)
            {
                item.Handle = new DxfHandle(nextHandle.Value);
                nextHandle = new DxfHandle(nextHandle.Value + 1);
            }

            if (!item.OwnerHandle.HasValue && ownerHandle.HasValue)
            {
                item.OwnerHandle = ownerHandle;
            }

            foreach (var child in item.GetPointers().Where(c => c.Item != null))
            {
                var childItem = child.Item as IDxfItemInternal;
                var parentHandle = GetParentHandle(item, childItem);
                nextHandle = AssignHandles(childItem, nextHandle, parentHandle, visitedItems);
                if (childItem is not null)
                {
                    child.Handle = childItem.Handle;
                    childItem.OwnerHandle = parentHandle;
                }
            }

            return nextHandle;
        }

        private static DxfHandle GetParentHandle(IDxfItemInternal parent, IDxfItemInternal? child)
        {
            if (child is DxfEntity && !(parent is DxfDictionary))
            {
                // entities can only be parented by a dictionary
                return default(DxfHandle);
            }

            return parent.Handle;
        }
    }
}
