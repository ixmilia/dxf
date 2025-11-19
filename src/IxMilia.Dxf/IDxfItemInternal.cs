using System.Collections.Generic;

namespace IxMilia.Dxf
{
    internal interface IDxfItemInternal : IDxfItem
    {
        DxfHandle Handle { get; set; }
        DxfHandle OwnerHandle { get; set; }
        void SetOwner(IDxfItem owner);
        IEnumerable<DxfPointer> GetPointers();
        IEnumerable<IDxfItemInternal> GetChildItems();
    }
}
