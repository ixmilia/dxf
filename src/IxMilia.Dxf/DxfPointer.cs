// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
            foreach (var item in file.GetFileItems())
            {
                if (item.Handle != 0u)
                {
                    handleMap[item.Handle] = item;
                }
            }

            // bind all pointers
            foreach (var item in file.GetFileItems())
            {
                BindPointers(item, handleMap);
            }
        }

        private static void BindPointers(IDxfItemInternal item, Dictionary<uint, IDxfItemInternal> handleMap)
        {
            foreach (var child in item.GetPointers())
            {
                if (handleMap.ContainsKey(child.Handle))
                {
                    child.Item = handleMap[child.Handle];
                    BindPointers((IDxfItemInternal)child.Item, handleMap);
                    ((IDxfItemInternal)child.Item).SetOwner(item);
                }
            }
        }

        public static uint AssignPointers(DxfFile file)
        {
            foreach (var item in file.GetFileItems())
            {
                item.Handle = 0u;
            }

            uint nextPointer = 1u;
            foreach (var item in file.GetFileItems().Where(i => i != null))
            {
                nextPointer = AssignPointers(item, nextPointer);
            }

            return nextPointer;
        }

        private static uint AssignPointers(IDxfItemInternal item, uint nextHandle)
        {
            if (item.Handle == 0u)
            {
                item.Handle = nextHandle++;
            }

            foreach (var child in item.GetPointers().Where(c => c.Item != null))
            {
                var childItem = (IDxfItemInternal)child.Item;
                nextHandle = AssignPointers(childItem, nextHandle);
                child.Handle = childItem.Handle;
                childItem.OwnerHandle = item.Handle;
            }

            return nextHandle++;
        }
    }
}
