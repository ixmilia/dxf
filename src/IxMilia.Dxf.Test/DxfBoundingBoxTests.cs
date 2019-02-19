using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfBoundingBoxTests
    {
        [Fact]
        public void ArcBoundingBox()
        {
            DxfPoint center = new DxfPoint(0, 0, 0);
            const double radius = 1.0;
            const double startAngle = 90;
            const double endAngle = 180;

            DxfArc sut = new DxfArc(center, radius, startAngle, endAngle);
            DxfBoundingBox? nullableBb = sut.GetBoundingBox();
            Assert.True(nullableBb.HasValue);
            DxfBoundingBox bb = nullableBb.Value;

            DxfPoint expectedMin = new DxfPoint(-1, 0, 0);
            DxfPoint expectedMax = new DxfPoint(0, 1, 0);
            Assert.Equal(expectedMin.X, bb.MinimumPoint.X, 10);
            Assert.Equal(expectedMin.Y, bb.MinimumPoint.Y, 10);
            Assert.Equal(expectedMin.X, bb.MinimumPoint.X, 10);
            Assert.Equal(expectedMax.Y, bb.MaximumPoint.Y, 10);
        }

        [Fact]
        public void PolyLineVertexBulgeIsRespected()
        {
            // Data from https://github.com/IxMilia/Dxf/issues/102
            var vertices = new[]
            {
                new DxfVertex(new DxfPoint(0, +0.5, 0)),
                new DxfVertex(new DxfPoint(-1.5, +0.5, 0)) {Bulge = 1},
                new DxfVertex(new DxfPoint(-1.5, -0.5, 0)),
                new DxfVertex(new DxfPoint(0, -0.5, 0)) {Bulge = 1}
            };
            DxfPolyline sut = new DxfPolyline(vertices) {IsClosed = false};
            DxfPoint expectedMin = new DxfPoint(-2.0, -0.5, 0);
            DxfPoint expectedMax = new DxfPoint(+0.5, +0.5, 0);
            DxfVector expectedSize = new DxfVector(2.5, 1.0, 0);

            DxfBoundingBox? nullableBb = sut.GetBoundingBox();
            Assert.True(nullableBb.HasValue);
            DxfBoundingBox bb = nullableBb.Value;

            Assert.Equal(expectedMin, bb.MinimumPoint);
            Assert.Equal(expectedMax, bb.MaximumPoint);
            Assert.Equal(expectedSize, bb.Size);
        }
    }
}