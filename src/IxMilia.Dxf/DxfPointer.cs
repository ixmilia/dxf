// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

            // bind all pointers
            foreach (var item in file.GetFileItems())
            {
                BindPointers(item, handleMap, visitedItems);
            }
        }

        private static void GatherPointers(IDxfItemInternal item, Dictionary<uint, IDxfItemInternal> handleMap, HashSet<IDxfItemInternal> visitedItems)
        {
            if (item != null && !visitedItems.Contains(item))
            {
                visitedItems.Add(item);
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

        private static void BindPointers(IDxfItemInternal item, Dictionary<uint, IDxfItemInternal> handleMap, HashSet<IDxfItemInternal> visitedItems)
        {
            if (!visitedItems.Contains(item))
            {
                visitedItems.Add(item);
                foreach (var child in item.GetPointers())
                {
                    if (handleMap.ContainsKey(child.Handle))
                    {
                        child.Item = handleMap[child.Handle];
                        BindPointers((IDxfItemInternal)child.Item, handleMap, visitedItems);
                        ((IDxfItemInternal)child.Item).SetOwner(item);
                    }
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
                nextPointer = AssignHandles(item, nextPointer, visitedItems);
            }

            return nextPointer;
        }

        private static uint AssignHandles(IDxfItemInternal item, uint nextHandle, HashSet<IDxfItemInternal> visitedItems)
        {
            if (item == null || visitedItems.Contains(item))
            {
                return nextHandle;
            }

            visitedItems.Add(item);
            Debug.Assert(item.Handle == 0u);
            item.Handle = nextHandle++;

            foreach (var child in item.GetPointers().Where(c => c.Item != null))
            {
                var childItem = (IDxfItemInternal)child.Item;
                nextHandle = AssignHandles(childItem, nextHandle, visitedItems);
                child.Handle = childItem.Handle;
                childItem.OwnerHandle = item.Handle;
            }

            return nextHandle++;
        }

        private static void ClearPointers(IDxfItemInternal item, HashSet<IDxfItemInternal> visitedItems)
        {
            if (item != null && !visitedItems.Contains(item))
            {
                visitedItems.Add(item);
                item.Handle = 0u;
                foreach (var child in item.GetChildItems())
                {
                    ClearPointers(child, visitedItems);
                }
            }
        }
    }
}
