using System.Collections.Generic;
#nullable enable

using System.Text;

namespace IxMilia.Dxf
{
    internal interface IDxfCodePairReader
    {
        IEnumerable<DxfCodePair> GetCodePairs();
        void SetReaderEncoding(Encoding encoding);
    }
}
