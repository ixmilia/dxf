namespace IxMilia.Dxf.Entities
{
    public partial class DxfProxyEntity
    {
        public byte[] GraphicsData { get; set; }

        public byte[] EntityData { get; set; }

        public int ObjectDrawingFormatVersion
        {
            // lower word
            get { return (int)(_objectDrawingFormat & 0xFFFF); }
            set { _objectDrawingFormat |= (uint)value & 0xFFFF; }
        }

        public int ObjectMaintenanceReleaseVersion
        {
            // upper word
            get { return (int)(_objectDrawingFormat >> 4); }
            set { _objectDrawingFormat = (uint)(value << 4) + _objectDrawingFormat & 0xFFFF; }
        }

        protected override DxfEntity PostParse()
        {
            GraphicsData = BinaryHelpers.CombineBytes(_graphicsDataBytes);
            _graphicsDataBytes.Clear();

            EntityData = BinaryHelpers.CombineBytes(_entityDataBytes);
            _entityDataBytes.Clear();

            return this;
        }
    }
}
