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
