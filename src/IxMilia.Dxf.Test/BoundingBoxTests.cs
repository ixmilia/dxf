using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class BoundingBoxTests
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

        [Fact]
        public void InsertBoundingBox()
        {
            var line = new DxfLine(new DxfPoint(1.0, 1.0, 0.0), new DxfPoint(2.0, 3.0, 0.0));
            var offset = new DxfVector(2.0, 2.0, 0.0);

            var block = new DxfBlock();
            block.Name = "some-block";
            block.Entities.Add(line);

            var insert = new DxfInsert();
            insert.Name = "some-block";
            insert.Location = offset;
            insert.XScaleFactor = 2.0;

            var file = new DxfFile();
            file.Blocks.Add(block);
            file.Entities.Add(insert);

            var boundingBox = file.GetBoundingBox();
            Assert.Equal(new DxfPoint(4.0, 3.0, 0.0), boundingBox.MinimumPoint);
            Assert.Equal(new DxfPoint(6.0, 5.0, 0.0), boundingBox.MaximumPoint);
        }
    }
}
