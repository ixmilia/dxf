using System;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfVbaProject
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();

        protected override DxfObject PostParse()
        {
            Data = BinaryHelpers.CombineBytes(_hexData);
            _hexData.Clear();
            return this;
        }
    }
}
