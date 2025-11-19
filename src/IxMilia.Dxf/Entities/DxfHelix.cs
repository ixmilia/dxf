using System.Collections.Generic;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfHelix
    {
        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            var radiusX = new DxfVector(Radius, 0.0, 0.0);
            var radiusY = new DxfVector(0.0, Radius, 0.0);

            // base of the helix
            yield return StartPoint + radiusX;
            yield return StartPoint - radiusX;
            yield return StartPoint + radiusY;
            yield return StartPoint - radiusY;

            // other side of the helix
            var endPoint = StartPoint + AxisVector.Normalize() * NumberOfTurns * TurnHeight;
            yield return endPoint + radiusX;
            yield return endPoint - radiusX;
            yield return endPoint + radiusY;
            yield return endPoint - radiusY;
        }
    }
}
