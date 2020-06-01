namespace IxMilia.Dxf.Entities
{
    public struct DxfControlPoint
    {
        public DxfPoint Point { get; set; }
        public double Weight { get; set; }

        public DxfControlPoint(DxfPoint point, double weight)
        {
            Point = point;
            Weight = weight;
        }

        public DxfControlPoint(DxfPoint point)
            : this(point, 1.0)
        {
        }
    }
}
