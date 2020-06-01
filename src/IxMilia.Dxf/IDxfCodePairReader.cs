using System.Collections.Generic;

namespace IxMilia.Dxf
{
    internal interface IDxfCodePairReader
    {
        IEnumerable<DxfCodePair> GetCodePairs();
        void SetUtf8Reader();
    }
}
