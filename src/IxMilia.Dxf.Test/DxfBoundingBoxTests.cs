using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfBoundingBoxTests
    {
        [Fact]
        public void ArcBoundingBox()
        {
            var radius = 1.0;
            var startAngle = 90;
            var endAngle = 180;

            var arc = new DxfArc(DxfPoint.Origin, radius, startAngle, endAngle);
            var bb = arc.GetBoundingBox().Value;

            var expectedMin = new DxfPoint(-1, 0, 0);
            var expectedMax = new DxfPoint(0, 1, 0);
            Assert.Equal(expectedMin.X, bb.MinimumPoint.X, 10);
            Assert.Equal(expectedMin.Y, bb.MinimumPoint.Y, 10);
            Assert.Equal(expectedMin.X, bb.MinimumPoint.X, 10);
            Assert.Equal(expectedMax.Y, bb.MaximumPoint.Y, 10);
        }

        [Fact]
        public void PolyLineVertexBulgeIsRespected()
        {
            // Data from https://github.com/ixmilia/dxf/issues/102
            var vertices = new[]
            {
                new DxfVertex(new DxfPoint(0, +0.5, 0)),
                new DxfVertex(new DxfPoint(-1.5, +0.5, 0)) {Bulge = 1},
                new DxfVertex(new DxfPoint(-1.5, -0.5, 0)),
                new DxfVertex(new DxfPoint(0, -0.5, 0)) {Bulge = 1}
            };
            var poly = new DxfPolyline(vertices) {IsClosed = false};
            var expectedMin = new DxfPoint(-2.0, -0.5, 0);
            var expectedMax = new DxfPoint(+0.5, +0.5, 0);
            var expectedSize = new DxfVector(2.5, 1.0, 0);

            var bb = poly.GetBoundingBox().Value;
            Assert.Equal(expectedMin, bb.MinimumPoint);
            Assert.Equal(expectedMax, bb.MaximumPoint);
            Assert.Equal(expectedSize, bb.Size);
        }
    }
}
