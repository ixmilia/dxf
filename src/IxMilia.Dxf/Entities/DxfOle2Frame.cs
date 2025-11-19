#nullable enable

using System;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfOle2Frame
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();

        protected override DxfEntity PostParse()
        {
            Data = BinaryHelpers.CombineBytes(_binaryDataStrings);
            _binaryDataStrings.Clear();
            return this;
        }
    }
}
