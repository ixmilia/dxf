using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class EntityExtensionTests
    {
        [Fact]
        public void ShouldContainStartAndEndAngle()
        {
            var radius = 1.0; // negative radius only for DxfVertex.Bulge, but not for arcs (?)
            for (var start = 0; start <= 360; start += 10)
            {
                for (var end = 0; end <= 360; end += 10)
                {
                    var arc = new DxfArc(DxfPoint.Origin, radius, start, end);
                    Assert.True(arc.ContainsAngle(start), $"{start}°=>{end}° arc contains {start}°");
                    Assert.True(arc.ContainsAngle(end), $"{start}°=>{end}° arc contains {end}°");
                }
            }
        }

        [Theory]
        // simple 90° arcs
        [InlineData(0, 90, 20)]
        [InlineData(90, 180, 110)]
        [InlineData(180, 270, 200)]
        [InlineData(270, 360, 290)]
        // large outer 270° arcs
        [InlineData(90, 0, 0)]
        [InlineData(90, 0, 95)]
        [InlineData(90, 0, 355)]
        [InlineData(90, 0, 360)]
        [InlineData(180, 90, 190)]
        [InlineData(180, 90, 355)] // before boundary
        [InlineData(180, 90, 5)] // after boundary
        [InlineData(180, 90, 85)]
        public void ShouldContainAngle(double startAngle, double endAngle, double angle)
        {
            var radius = 1.0; // negative radius only for DxfVertex.Bulge, but not for arcs (?)
            var arc = new DxfArc(DxfPoint.Origin, radius, startAngle, endAngle);
            Assert.True(arc.ContainsAngle(angle));
        }

        [Theory]
        // simple 90° arcs
        [InlineData(0, 90, 359.9)]
        [InlineData(90, 180, 0)]
        [InlineData(180, 270, 360)]
        [InlineData(270, 360, 80)]
        // large outer 270° arcs cross boundary
        [InlineData(90, 0, 45)]
        [InlineData(180, 90, 91)]
        [InlineData(180, 90, 179)]
        public void ShouldNotContainAngle(double startAngle, double endAngle, double angle)
        {
            var radius = 1.0;
            var arc = new DxfArc(DxfPoint.Origin, radius, startAngle, endAngle);
            Assert.False(arc.ContainsAngle(angle));
        }

        [Theory]
        [InlineData(0.0, 5.0, 0.0, 0.0)]
        [InlineData(3.1415926535897931 / 2, 0.0, 2.5, 0.0)]
        [InlineData(3.1415926535897931, -5.0, 0.0, 0.0)]
        [InlineData(3.1415926535897931 * (3.0 / 2.0), 0, -2.5, 0.0)]
        public void GetPointFromAngle_CardinalRotationReturnsCorrectPoint(double angle, double expectedX, double expectedY, double expectedZ)
        {
            var ellipse = new DxfEllipse
            {
                Center = new DxfPoint(0, 0, 0),
                MajorAxis = new DxfVector(5, 0, 0),
                MinorAxisRatio = 0.5,
                Normal = new DxfVector(0, 0, 1)
            };

            var result = ellipse.GetPointFromAngle(angle);

            DXFPointsEqual(new DxfPoint(expectedX, expectedY, expectedZ), result);
        }

        [Fact]
        public void GetPointFromAngle_ZAxisIsApplied()
        {
            // An ellipse on the XZ plane
            var ellipse = new DxfEllipse
            {
                Center = new DxfPoint(0, 0, 0),
                MajorAxis = new DxfVector(5, 0, 0),
                MinorAxisRatio = 0.5,
                Normal = new DxfVector(0, 1, 0)
            };

            var result = ellipse.GetPointFromAngle(3.1415926535897931 / 2);

            DXFPointsEqual(new DxfPoint(0, 0, -2.5), result);
        }

        private static void DXFPointsEqual(DxfPoint expected, DxfPoint actual, double tolerance = 1e-10)
        {
            Assert.Equal(expected.X, actual.X, tolerance);
            Assert.Equal(expected.Y, actual.Y, tolerance);
            Assert.Equal(expected.Z, actual.Z, tolerance);
        }
    }
}
