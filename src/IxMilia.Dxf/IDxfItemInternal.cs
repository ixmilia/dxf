using System.Collections.Generic;

namespace IxMilia.Dxf
{
    internal interface IDxfItemInternal : IDxfItem
    {
        IEnumerable<DxfPointer> GetPointers();
        IEnumerable<IDxfItemInternal> GetChildItems();
    }
}
