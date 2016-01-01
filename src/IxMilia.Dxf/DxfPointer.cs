// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf
{
    internal class DxfPointer
    {
        public uint Handle { get; set; }
        public IDxfItem Item { get; set; }

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
            var handleMap = new Dictionary<uint, IDxfItem>();
            foreach (var item in file.GetFileItems())
            {
                var hasHandle = item as IDxfHasHandle;
                if (hasHandle?.Handle != 0u)
                {
                    handleMap[hasHandle.Handle] = item;
                }
            }

            // bind all pointers
            foreach (var item in file.GetFileItems())
            {
                BindPointers(item, handleMap);
            }
        }

        private static void BindPointers(IDxfItem item, Dictionary<uint, IDxfItem> handleMap)
        {
            var hasChildren = item as IDxfHasChildPointers;
            if (hasChildren != null)
            {
                foreach (var child in hasChildren.GetChildPointers())
                {
                    if (handleMap.ContainsKey(child.Handle))
                    {
                        child.Item = handleMap[child.Handle];
                        BindPointers(child.Item, handleMap);
                        var hasOwner = child.Item as IDxfHasOwnerInternal;
                        if (hasOwner != null)
                        {
                            hasOwner.SetOwner(item);
                        }
                    }
                }
            }
        }

        public static void AssignPointers(DxfFile file)
        {
            foreach (var item in file.GetFileItems().Cast<IDxfHasHandle>())
            {
                item.Handle = 0u;
            }

            uint nextPointer = 1u;
            foreach (var item in file.GetFileItems().Cast<IDxfHasHandle>())
            {
                nextPointer = AssignPointers(item, nextPointer);
            }
        }

        private static uint AssignPointers(IDxfHasHandle item, uint nextHandle)
        {
            if (item.Handle == 0u)
            {
                item.Handle = nextHandle++;
            }

            var hasChildren = item as IDxfHasChildPointers;
            if (hasChildren != null)
            {
                foreach (var child in hasChildren.GetChildPointers())
                {
                    nextHandle = AssignPointers((IDxfHasHandle)child.Item, nextHandle);
                    child.Handle = ((IDxfHasHandle)child.Item).Handle;
                    var hasOwnerHandle = child.Item as IDxfHasOwnerHandle;
                    if (hasOwnerHandle != null)
                    {
                        hasOwnerHandle.OwnerHandle = item.Handle;
                    }
                }
            }

            return nextHandle++;
        }
    }
}
