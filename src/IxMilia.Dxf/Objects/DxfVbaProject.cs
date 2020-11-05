namespace IxMilia.Dxf.Objects
{
    public partial class DxfVbaProject
    {
        public byte[] Data { get; set; }

        protected override DxfObject PostParse()
        {
            Data = BinaryHelpers.CombineBytes(_hexData);
            _hexData.Clear();
            return this;
        }
    }
}
