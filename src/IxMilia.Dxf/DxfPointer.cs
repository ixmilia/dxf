// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Objects;

namespace IxMilia.Dxf
{
    internal class DxfPointer
    {
        public uint Handle { get; set; }
        public IDxfItem Item { get; set; }

        public DxfPointer()
            : this(0u, null)
        {
        }

        public DxfPointer(uint handle)
            : this(handle, null)
        {
        }

        public DxfPointer(IDxfItem item)
            : this(0u, item)
        {
        }

        public DxfPointer(uint handle, IDxfItem item)
        {
            Handle = handle;
            Item = item;
        }

        public static void BindPointers(DxfFile file)
        {
            // gather all items by handle
            var handleMap = new Dictionary<uint, IDxfItemInternal>();
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

        private static void GatherPointers(IDxfItemInternal item, Dictionary<uint, IDxfItemInternal> handleMap, HashSet<IDxfItemInternal> visitedItems)
        {
            if (item != null && visitedItems.Add(item))
            {
                if (item.Handle != 0u)
                {
                    handleMap[item.Handle] = item;
                }

                foreach (var child in item.GetChildItems())
                {
                    GatherPointers(child, handleMap, visitedItems);
                }
            }
        }

        private static void BindPointers(IDxfItemInternal item, Dictionary<uint, IDxfItemInternal> handleMap, HashSet<IDxfItemInternal> visitedItems, HashSet<IDxfItemInternal> visitedChildren)
        {
            if (visitedItems.Add(item))
            {
                // set initial owners
                SetChildOwners(item, visitedChildren);

                // set explicit owners
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
                    pair.Value = DxfCommonConverters.UIntHandle(owner.Handle);
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

        public static uint AssignHandles(DxfFile file)
        {
            var visitedItems = new HashSet<IDxfItemInternal>();
            foreach (var item in file.GetFileItems())
            {
                ClearPointers(item, visitedItems);
            }

            visitedItems.Clear();

            uint nextPointer = 1u;
            foreach (var item in file.GetFileItems().Where(i => i != null))
            {
                nextPointer = AssignHandles(item, nextPointer, 0u, visitedItems);
                var isParentDictionary = item is DxfDictionary;
                foreach (var child in item.GetChildItems().Where(c => c != null))
                {
                    var parentHandle = GetParentHandle(item, child);
                    nextPointer = AssignHandles(child, nextPointer, parentHandle, visitedItems);
                    SetOwner(child, item, isWriting: true);
                }
            }

            return nextPointer;
        }

        private static uint AssignHandles(IDxfItemInternal item, uint nextHandle, uint ownerHandle, HashSet<IDxfItemInternal> visitedItems)
        {
            if (item == null || !visitedItems.Add(item))
            {
                return nextHandle;
            }

            Debug.Assert(item.Handle == 0u);
            item.Handle = nextHandle++;
            if (item.OwnerHandle == 0u && ownerHandle != 0u)
            {
                item.OwnerHandle = ownerHandle;
            }

            foreach (var child in item.GetPointers().Where(c => c.Item != null))
            {
                var childItem = (IDxfItemInternal)child.Item;
                var parentHandle = GetParentHandle(item, childItem);
                nextHandle = AssignHandles(childItem, nextHandle, parentHandle, visitedItems);
                child.Handle = childItem.Handle;
                childItem.OwnerHandle = parentHandle;
            }

            return nextHandle;
        }

        private static uint GetParentHandle(IDxfItemInternal parent, IDxfItemInternal child)
        {
            if (child is DxfEntity && !(parent is DxfDictionary))
            {
                // entities can only be parented by a dictionary
                return 0u;
            }

            return parent.Handle;
        }

        private static void ClearPointers(IDxfItemInternal item, HashSet<IDxfItemInternal> visitedItems)
        {
            if (item != null && visitedItems.Add(item))
            {
                item.Handle = 0u;
                foreach (var child in item.GetChildItems())
                {
                    ClearPointers(child, visitedItems);
                }
            }
        }
    }
}
