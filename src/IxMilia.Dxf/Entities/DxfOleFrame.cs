namespace IxMilia.Dxf.Entities
{
    public partial class DxfOleFrame
    {
        public byte[] Data { get; set; }

        protected override DxfEntity PostParse()
        {
            Data = BinaryHelpers.CombineBytes(_binaryDataStrings);
            _binaryDataStrings.Clear();
            return this;
        }
    }
}
