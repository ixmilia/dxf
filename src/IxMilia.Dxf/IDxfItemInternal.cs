// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace IxMilia.Dxf
{
    internal interface IDxfItemInternal : IDxfItem
    {
        uint Handle { get; set; }
        uint OwnerHandle { get; set; }
        void SetOwner(IDxfItem owner);
        IEnumerable<DxfPointer> GetPointers();
        IEnumerable<IDxfItemInternal> GetChildItems();
    }
}
