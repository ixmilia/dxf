using System;
using System.Linq;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Sections;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class EntityTests : AbstractDxfTests
    {
        private static DxfEntity Entity(string entityType, params (int code, object value)[] codePairs)
        {
            var preCodePairs = new[]
            {
                (999, (object)"ill-placed-comment"),
                (0, entityType),
                (5, "42"),
                (6, "<line-type-name>"),
                (8, "<layer>"),
                (48, 3.14159),
                (60, 1),
                (62, 1),
                (67, 1),
            };
            var testBuffer = new TestCodePairBufferReader(preCodePairs.Concat(codePairs));
            var bufferReader = new DxfCodePairBufferReader(testBuffer);
            var unusedFile = new DxfFile();
            var entitiesSection = DxfEntitiesSection.EntitiesSectionFromBuffer(bufferReader, unusedFile);
            var entity = entitiesSection.Entities.Single();
            Assert.Equal(new DxfHandle(0x42), ((IDxfItemInternal)entity).Handle);
            Assert.Equal("<line-type-name>", entity.LineTypeName);
            Assert.Equal("<layer>", entity.Layer);
            Assert.Equal(3.14159, entity.LineTypeScale);
            Assert.False(entity.IsVisible);
            Assert.True(entity.IsInPaperSpace);
            Assert.Equal(DxfColor.FromIndex(1), entity.Color);
            return entity;
        }

        private static DxfEntity EmptyEntity(string entityType)
        {
            var file = Section("ENTITIES",
                (0, entityType)
            );
            var entity = file.Entities.Single();
            Assert.Equal("0", entity.Layer);
            Assert.Equal("BYLAYER", entity.LineTypeName);
            Assert.Equal(1.0, entity.LineTypeScale);
            Assert.True(entity.IsVisible);
            Assert.False(entity.IsInPaperSpace);
            Assert.Equal(DxfColor.ByLayer, entity.Color);
            return entity;
        }

        private static void EnsureFileContainsEntity(DxfEntity entity, params (int code, object value)[] codePairs)
        {
            EnsureFileContainsEntity(entity, DxfAcadVersion.R12, codePairs);
        }

        private static void EnsureFileContainsEntity(DxfEntity entity, DxfAcadVersion version, params (int code, object value)[] codePairs)
        {
            var file = new DxfFile();
            file.Header.Version = version;
            file.Entities.Add(entity);
            VerifyFileContains(file, DxfSectionType.Entities, codePairs);
        }

        private static void EnsureFileDoesNotContainWithEntity(DxfEntity entity, DxfAcadVersion version, params (int code, object value)[] codePairs)
        {
            var file = new DxfFile();
            file.Header.Version = version;
            file.Entities.Add(entity);
            VerifyFileDoesNotContain(file, DxfSectionType.Entities, codePairs);
        }

        [Fact]
        public void ReadEntityPreviewImageDataTest()
        {
            var e = Entity("LINE",
                (310, new byte[] { 0x12, 0x34 }),
                (310, new byte[] { 0x56, 0x78 })
            );
            Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 }, e.PreviewImageData);
        }

        [Fact]
        public void ReadEntityPreviewImageDataFromEntityThatReturnsNewEntityOnPostParseTest()
        {
            var e = Entity("DIMENSION",
                (100, "AcDbAlignedDimension"),
                (310, new byte[] { 0x12, 0x34 }),
                (310, new byte[] { 0x56, 0x78 })
            );
            Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 }, e.PreviewImageData);
        }

        [Fact]
        public void EmptyPreviewImageDataDoesntGetWrittenTest()
        {
            var e = new DxfLine();
            e.PreviewImageData = null;
            EnsureFileDoesNotContainWithEntity(e, DxfAcadVersion.R2000,
                (92, 0)
            );
        }

        [Fact]
        public void PreviewImageDataDoesntGetWrittenWhenPresentButOnR14OrLessTest()
        {
            var e = new DxfLine();
            e.PreviewImageData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            EnsureFileDoesNotContainWithEntity(e, DxfAcadVersion.R14,
                (92, 4)
            );
            EnsureFileDoesNotContainWithEntity(e, DxfAcadVersion.R14,
                (310, new byte[] { 0x12, 0x34, 0x56, 0x78 })
            );
        }

        [Fact]
        public void PreviewImageDataGetsWrittenWhenPresentTest()
        {
            var e = new DxfLine();
            e.PreviewImageData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            EnsureFileContainsEntity(e, DxfAcadVersion.R2000,
                (92, 4),
                (310, new byte[] { 0x12, 0x34, 0x56, 0x78 })
            );
        }

        [Fact]
        public void ReadEntityExtensionDataTest()
        {
            var line = (DxfLine)Entity("LINE",
                (102, "{APP_NAME"),
                (360, "AAAA"),
                (360, "BBBB"),
                (102, "}")
            );
            var group = line.ExtensionDataGroups.Single();
            Assert.Equal("APP_NAME", group.GroupName);
            Assert.Equal(2, group.Items.Count);
            Assert.Equal(new DxfCodePair(360, "AAAA"), group.Items[0]);
            Assert.Equal(new DxfCodePair(360, "BBBB"), group.Items[1]);
        }

        [Fact]
        public void WriteEntityExtensionDataTest()
        {
            var line = new DxfLine();
            line.ExtensionDataGroups.Add(new DxfCodePairGroup("APP_NAME", new IDxfCodePairOrGroup[]
            {
                new DxfCodePair(1, "foo"),
                new DxfCodePair(2, "bar")
            }));
            EnsureFileContainsEntity(line,
                DxfAcadVersion.R14,
                (0, "LINE"),
                (5, "#"),
                (102, "{APP_NAME"),
                (1, "foo"),
                (2, "bar"),
                (102, "}"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbLine"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (11, 0.0),
                (21, 0.0),
                (31, 0.0)
            );
        }

        [Fact]
        public void WriteEntityWithNullLayerTest()
        {
            var line = new DxfLine() { Layer = "" };
            EnsureFileContainsEntity(line,
                (0, "LINE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0")
            );
        }

        [Fact]
        public void DimensionDefaultValuesTest()
        {
            var dim = new DxfAlignedDimension();
            Assert.Null(dim.BlockName);
            Assert.Equal("STANDARD", dim.DimensionStyleName);
        }

        [Fact]
        public void RTextDefaultValuesTest()
        {
            var rtext = new DxfRText();
            Assert.Equal("STANDARD", rtext.TextStyle);
        }

        [Fact]
        public void SplineDefaultValuesTest()
        {
            var spline = new DxfSpline();
            Assert.Equal(1, spline.DegreeOfCurve);
        }

        [Fact]
        public void RightVectorFromNegativeNormalTest()
        {
            // gleaned from https://github.com/ixmilia/dxf/issues/105
            // A 90 degree arc from 270-360 degrees with a negative Z normal is displayed by both AutoCAD and ODA in Q3.
            var right = DxfVector.RightVectorFromNormal(new DxfVector(0.0, 0.0, -1.0));
            var expected = -DxfVector.XAxis;
            Assert.Equal(expected, right);
        }

        [Fact]
        public void ReadDefaultLineTest()
        {
            var line = (DxfLine)EmptyEntity("LINE");
            Assert.Equal(0.0, line.P1.X);
            Assert.Equal(0.0, line.P1.Y);
            Assert.Equal(0.0, line.P1.Z);
            Assert.Equal(0.0, line.P2.X);
            Assert.Equal(0.0, line.P2.Y);
            Assert.Equal(0.0, line.P2.Z);
            Assert.Equal(0.0, line.Thickness);
            Assert.Equal(0.0, line.ExtrusionDirection.X);
            Assert.Equal(0.0, line.ExtrusionDirection.Y);
            Assert.Equal(1.0, line.ExtrusionDirection.Z);
        }

        [Fact]
        public void ReadDefaultCircleTest()
        {
            var circle = (DxfCircle)EmptyEntity("CIRCLE");
            Assert.Equal(0.0, circle.Center.X);
            Assert.Equal(0.0, circle.Center.Y);
            Assert.Equal(0.0, circle.Center.Z);
            Assert.Equal(0.0, circle.Radius);
            Assert.Equal(0.0, circle.Normal.X);
            Assert.Equal(0.0, circle.Normal.Y);
            Assert.Equal(1.0, circle.Normal.Z);
            Assert.Equal(0.0, circle.Thickness);
        }

        [Fact]
        public void ReadDefaultArcTest()
        {
            var arc = (DxfArc)EmptyEntity("ARC");
            Assert.Equal(0.0, arc.Center.X);
            Assert.Equal(0.0, arc.Center.Y);
            Assert.Equal(0.0, arc.Center.Z);
            Assert.Equal(0.0, arc.Radius);
            Assert.Equal(0.0, arc.Normal.X);
            Assert.Equal(0.0, arc.Normal.Y);
            Assert.Equal(1.0, arc.Normal.Z);
            Assert.Equal(0.0, arc.StartAngle);
            Assert.Equal(360.0, arc.EndAngle);
            Assert.Equal(0.0, arc.Thickness);
        }

        [Fact]
        public void ReadDefaultEllipseTest()
        {
            var el = (DxfEllipse)EmptyEntity("ELLIPSE");
            Assert.Equal(0.0, el.Center.X);
            Assert.Equal(0.0, el.Center.Y);
            Assert.Equal(0.0, el.Center.Z);
            Assert.Equal(1.0, el.MajorAxis.X);
            Assert.Equal(0.0, el.MajorAxis.Y);
            Assert.Equal(0.0, el.MajorAxis.Z);
            Assert.Equal(0.0, el.Normal.X);
            Assert.Equal(0.0, el.Normal.Y);
            Assert.Equal(1.0, el.Normal.Z);
            Assert.Equal(1.0, el.MinorAxisRatio);
            Assert.Equal(0.0, el.StartParameter);
            Assert.Equal(Math.PI * 2, el.EndParameter);
        }

        [Fact]
        public void ReadDefaultTextTest()
        {
            var text = (DxfText)EmptyEntity("TEXT");
            Assert.Equal(0.0, text.Location.X);
            Assert.Equal(0.0, text.Location.Y);
            Assert.Equal(0.0, text.Location.Z);
            Assert.Equal(0.0, text.Normal.X);
            Assert.Equal(0.0, text.Normal.Y);
            Assert.Equal(1.0, text.Normal.Z);
            Assert.Equal(0.0, text.Rotation);
            Assert.Equal(1.0, text.TextHeight);
            Assert.Null(text.Value);
            Assert.Equal("STANDARD", text.TextStyleName);
            Assert.Equal(0.0, text.Thickness);
            Assert.Equal(1.0, text.RelativeXScaleFactor);
            Assert.Equal(0.0, text.ObliqueAngle);
            Assert.False(text.IsTextBackward);
            Assert.False(text.IsTextUpsideDown);
            Assert.Equal(0.0, text.SecondAlignmentPoint.X);
            Assert.Equal(0.0, text.SecondAlignmentPoint.Y);
            Assert.Equal(0.0, text.SecondAlignmentPoint.Z);
            Assert.Equal(DxfHorizontalTextJustification.Left, text.HorizontalTextJustification);
            Assert.Equal(DxfVerticalTextJustification.Baseline, text.VerticalTextJustification);
        }

        [Fact]
        public void ReadDefaultVertexTest()
        {
            var vertex = (DxfVertex)EmptyEntity("VERTEX");
            Assert.Equal(0.0, vertex.Location.X);
            Assert.Equal(0.0, vertex.Location.Y);
            Assert.Equal(0.0, vertex.Location.Z);
            Assert.Equal(0.0, vertex.StartingWidth);
            Assert.Equal(0.0, vertex.EndingWidth);
            Assert.Equal(0.0, vertex.Bulge);
            Assert.False(vertex.IsExtraCreatedByCurveFit);
            Assert.False(vertex.IsCurveFitTangentDefined);
            Assert.False(vertex.IsSplineVertexCreatedBySplineFitting);
            Assert.False(vertex.IsSplineFrameControlPoint);
            Assert.False(vertex.Is3DPolylineVertex);
            Assert.False(vertex.Is3DPolygonMesh);
            Assert.False(vertex.IsPolyfaceMeshVertex);
            Assert.Equal(0.0, vertex.CurveFitTangentDirection);
            Assert.Equal(0, vertex.PolyfaceMeshVertexIndex1);
            Assert.Equal(0, vertex.PolyfaceMeshVertexIndex2);
            Assert.Equal(0, vertex.PolyfaceMeshVertexIndex3);
            Assert.Equal(0, vertex.PolyfaceMeshVertexIndex4);
        }

        [Fact]
        public void ReadDefaultSeqendTest()
        {
            var seqend = (DxfSeqend)EmptyEntity("SEQEND");
            // nothing to verify
        }

        [Fact]
        public void ReadDefaultPolylineTest()
        {
            var poly = (DxfPolyline)EmptyEntity("POLYLINE");
            Assert.Equal(0.0, poly.Elevation);
            Assert.Equal(0.0, poly.Normal.X);
            Assert.Equal(0.0, poly.Normal.Y);
            Assert.Equal(1.0, poly.Normal.Z);
            Assert.Equal(0.0, poly.Thickness);
            Assert.Equal(0.0, poly.DefaultStartingWidth);
            Assert.Equal(0.0, poly.DefaultEndingWidth);
            Assert.Equal(0, poly.PolygonMeshMVertexCount);
            Assert.Equal(0, poly.PolygonMeshNVertexCount);
            Assert.Equal(0, poly.SmoothSurfaceMDensity);
            Assert.Equal(0, poly.SmoothSurfaceNDensity);
            Assert.Equal(DxfPolylineCurvedAndSmoothSurfaceType.None, poly.SurfaceType);
            Assert.False(poly.IsClosed);
            Assert.False(poly.CurveFitVerticesAdded);
            Assert.False(poly.SplineFitVerticesAdded);
            Assert.False(poly.Is3DPolyline);
            Assert.False(poly.Is3DPolygonMesh);
            Assert.False(poly.IsPolygonMeshClosedInNDirection);
            Assert.False(poly.IsPolyfaceMesh);
            Assert.False(poly.IsLineTypePatternGeneratedContinuously);
        }

        [Fact]
        public void ReadDefaultSolidTest()
        {
            var solid = (DxfSolid)EmptyEntity("SOLID");
            Assert.Equal(DxfPoint.Origin, solid.FirstCorner);
            Assert.Equal(DxfPoint.Origin, solid.SecondCorner);
            Assert.Equal(DxfPoint.Origin, solid.ThirdCorner);
            Assert.Equal(DxfPoint.Origin, solid.FourthCorner);
            Assert.Equal(0.0, solid.Thickness);
            Assert.Equal(DxfVector.ZAxis, solid.ExtrusionDirection);
        }

        [Fact]
        public void ReadLineTest()
        {
            var line = (DxfLine)Entity("LINE",
                (10, 11.0), // p1
                (20, 22.0),
                (30, 33.0),
                (11, 44.0), // p2
                (21, 55.0),
                (31, 66.0),
                (39, 77.0), // thickness
                (210, 88.0), // extrusion
                (220, 99.0),
                (230, 150.0)
            );
            Assert.Equal(11.0, line.P1.X);
            Assert.Equal(22.0, line.P1.Y);
            Assert.Equal(33.0, line.P1.Z);
            Assert.Equal(44.0, line.P2.X);
            Assert.Equal(55.0, line.P2.Y);
            Assert.Equal(66.0, line.P2.Z);
            Assert.Equal(77.0, line.Thickness);
            Assert.Equal(88.0, line.ExtrusionDirection.X);
            Assert.Equal(99.0, line.ExtrusionDirection.Y);
            Assert.Equal(150.0, line.ExtrusionDirection.Z);
        }

        [Fact]
        public void ReadCircleTest()
        {
            var circle = (DxfCircle)Entity("CIRCLE",
                (10, 11.0), // center
                (20, 22.0),
                (30, 33.0),
                (40, 44.0), // radius
                (39, 35.0), // thickness
                (210, 55.0), // normal
                (220, 66.0),
                (230, 77.0)
            );
            Assert.Equal(11.0, circle.Center.X);
            Assert.Equal(22.0, circle.Center.Y);
            Assert.Equal(33.0, circle.Center.Z);
            Assert.Equal(44.0, circle.Radius);
            Assert.Equal(55.0, circle.Normal.X);
            Assert.Equal(66.0, circle.Normal.Y);
            Assert.Equal(77.0, circle.Normal.Z);
            Assert.Equal(35.0, circle.Thickness);
        }

        [Fact]
        public void ReadArcTest()
        {
            var arc = (DxfArc)Entity("ARC",
                (10, 11.0), // center
                (20, 22.0),
                (30, 33.0),
                (40, 44.0), // radius
                (210, 55.0), // normal
                (220, 66.0),
                (230, 77.0),
                (50, 88.0), // start angle
                (51, 99.0), // end angle
                (39, 35.0) // thickness
            );
            Assert.Equal(11.0, arc.Center.X);
            Assert.Equal(22.0, arc.Center.Y);
            Assert.Equal(33.0, arc.Center.Z);
            Assert.Equal(44.0, arc.Radius);
            Assert.Equal(55.0, arc.Normal.X);
            Assert.Equal(66.0, arc.Normal.Y);
            Assert.Equal(77.0, arc.Normal.Z);
            Assert.Equal(88.0, arc.StartAngle);
            Assert.Equal(99.0, arc.EndAngle);
            Assert.Equal(35.0, arc.Thickness);
        }

        [Fact]
        public void ReadEllipseTest()
        {
            var el = (DxfEllipse)Entity("ELLIPSE",
                (10, 11.0), // center
                (20, 22.0),
                (30, 33.0),
                (11, 44.0), // major axis
                (21, 55.0),
                (31, 66.0),
                (210, 77.0), // normal
                (220, 88.0),
                (230, 99.0),
                (40, 12.0), // minor axis ratio
                (41, 0.1), // start parameter
                (42, 0.4) // end parameter
            );
            Assert.Equal(11.0, el.Center.X);
            Assert.Equal(22.0, el.Center.Y);
            Assert.Equal(33.0, el.Center.Z);
            Assert.Equal(44.0, el.MajorAxis.X);
            Assert.Equal(55.0, el.MajorAxis.Y);
            Assert.Equal(66.0, el.MajorAxis.Z);
            Assert.Equal(77.0, el.Normal.X);
            Assert.Equal(88.0, el.Normal.Y);
            Assert.Equal(99.0, el.Normal.Z);
            Assert.Equal(12.0, el.MinorAxisRatio);
            Assert.Equal(0.1, el.StartParameter);
            Assert.Equal(0.4, el.EndParameter);
        }

        [Fact]
        public void ReadTextTest()
        {
            var text = (DxfText)Entity("TEXT",
                (1, "foo bar"), // value
                (7, "text style name"),
                (10, 11.0), // location
                (20, 22.0),
                (30, 33.0),
                (39, 39.0), // thickness
                (40, 44.0), // text height
                (41, 41.0), // relative x scale factor
                (50, 55.0), // rotation
                (51, 51.0), // oblique angle
                (71, 255), // flags
                (72, 3), // horizontal justification
                (73, 1), // vertical justification
                (11, 91.0), // second alignment point
                (21, 92.0),
                (31, 93.0),
                (210, 66.0), // normal
                (220, 77.0),
                (230, 88.0)
            );
            Assert.Equal("foo bar", text.Value);
            Assert.Equal("text style name", text.TextStyleName);
            Assert.Equal(11.0, text.Location.X);
            Assert.Equal(22.0, text.Location.Y);
            Assert.Equal(33.0, text.Location.Z);
            Assert.Equal(39.0, text.Thickness);
            Assert.Equal(41.0, text.RelativeXScaleFactor);
            Assert.Equal(44.0, text.TextHeight);
            Assert.Equal(51.0, text.ObliqueAngle);
            Assert.True(text.IsTextBackward);
            Assert.True(text.IsTextUpsideDown);
            Assert.Equal(DxfHorizontalTextJustification.Aligned, text.HorizontalTextJustification);
            Assert.Equal(DxfVerticalTextJustification.Bottom, text.VerticalTextJustification);
            Assert.Equal(91.0, text.SecondAlignmentPoint.X);
            Assert.Equal(92.0, text.SecondAlignmentPoint.Y);
            Assert.Equal(93.0, text.SecondAlignmentPoint.Z);
            Assert.Equal(55.0, text.Rotation);
            Assert.Equal(66.0, text.Normal.X);
            Assert.Equal(77.0, text.Normal.Y);
            Assert.Equal(88.0, text.Normal.Z);
        }

        [Fact]
        public void ReadVertexTest()
        {
            var vertex = (DxfVertex)Entity("VERTEX",
                (10, 11.0), // location
                (20, 22.0),
                (30, 33.0),
                (40, 40.0), // starting width
                (41, 41.0), // ending width
                (42, 42.0), // bulge
                (50, 50.0), // curve tangent fit direction
                (70, 255), // flags
                (71, 71), // vertex indices
                (72, 72),
                (73, 73),
                (74, 74)
            );
            Assert.Equal(11.0, vertex.Location.X);
            Assert.Equal(22.0, vertex.Location.Y);
            Assert.Equal(33.0, vertex.Location.Z);
            Assert.Equal(40.0, vertex.StartingWidth);
            Assert.Equal(41.0, vertex.EndingWidth);
            Assert.Equal(42.0, vertex.Bulge);
            Assert.True(vertex.IsExtraCreatedByCurveFit);
            Assert.True(vertex.IsCurveFitTangentDefined);
            Assert.True(vertex.IsSplineVertexCreatedBySplineFitting);
            Assert.True(vertex.IsSplineFrameControlPoint);
            Assert.True(vertex.Is3DPolylineVertex);
            Assert.True(vertex.Is3DPolygonMesh);
            Assert.True(vertex.IsPolyfaceMeshVertex);
            Assert.Equal(50.0, vertex.CurveFitTangentDirection);
            Assert.Equal(71, vertex.PolyfaceMeshVertexIndex1);
            Assert.Equal(72, vertex.PolyfaceMeshVertexIndex2);
            Assert.Equal(73, vertex.PolyfaceMeshVertexIndex3);
            Assert.Equal(74, vertex.PolyfaceMeshVertexIndex4);
        }

        [Fact]
        public void ReadSeqendTest()
        {
            var seqend = (DxfSeqend)Entity("SEQEND");
            // nothing to verify
        }

        [Fact]
        public void ReadPolylineTest()
        {
            var poly = (DxfPolyline)Entity("POLYLINE",
                (30, 11.0), // elevation
                (39, 18.0), // thickness
                (40, 40.0), // starting width
                (41, 41.0), // ending width
                (70, 255), // flags
                (71, 71), // mesh m vertex count
                (72, 72), // mesh n vertex count
                (73, 73), // smooth surface m density
                (74, 74), // smooth surface n density
                (75, 6), // surface type = cubic b spline
                (250, 2), // polyline type = outline
                (210, 22.0), // normal
                (220, 33.0),
                (230, 44.0),
                (0, "VERTEX"),
                (10, 12.0),
                (20, 23.0),
                (30, 34.0),
                (0, "VERTEX"),
                (10, 45.0),
                (20, 56.0),
                (30, 67.0),
                (0, "SEQEND")
            );
            Assert.Equal(11.0, poly.Elevation);
            Assert.Equal(18.0, poly.Thickness);
            Assert.Equal(40.0, poly.DefaultStartingWidth);
            Assert.Equal(41.0, poly.DefaultEndingWidth);
            Assert.Equal(71, poly.PolygonMeshMVertexCount);
            Assert.Equal(72, poly.PolygonMeshNVertexCount);
            Assert.Equal(73, poly.SmoothSurfaceMDensity);
            Assert.Equal(74, poly.SmoothSurfaceNDensity);
            Assert.Equal(DxfPolylineType.Outline, poly.CLO_PolylineType);
            Assert.Equal(DxfPolylineCurvedAndSmoothSurfaceType.CubicBSpline, poly.SurfaceType);
            Assert.True(poly.IsClosed);
            Assert.True(poly.CurveFitVerticesAdded);
            Assert.True(poly.SplineFitVerticesAdded);
            Assert.True(poly.Is3DPolyline);
            Assert.True(poly.Is3DPolygonMesh);
            Assert.True(poly.IsPolygonMeshClosedInNDirection);
            Assert.True(poly.IsPolyfaceMesh);
            Assert.True(poly.IsLineTypePatternGeneratedContinuously);
            Assert.Equal(22.0, poly.Normal.X);
            Assert.Equal(33.0, poly.Normal.Y);
            Assert.Equal(44.0, poly.Normal.Z);
            Assert.Equal(2, poly.Vertices.Count);
            Assert.Equal(poly, poly.Vertices[0].Owner);
            Assert.Equal(poly, poly.Vertices[1].Owner);
            Assert.Equal(12.0, poly.Vertices[0].Location.X);
            Assert.Equal(23.0, poly.Vertices[0].Location.Y);
            Assert.Equal(34.0, poly.Vertices[0].Location.Z);
            Assert.Equal(45.0, poly.Vertices[1].Location.X);
            Assert.Equal(56.0, poly.Vertices[1].Location.Y);
            Assert.Equal(67.0, poly.Vertices[1].Location.Z);
        }

        [Fact]
        public void ReadSolidTest()
        {
            var solid = (DxfSolid)Entity("SOLID",
                (10, 1.0), // first corner
                (20, 2.0),
                (30, 3.0),
                (11, 4.0), // second corner
                (21, 5.0),
                (31, 6.0),
                (12, 7.0), // third corner
                (22, 8.0),
                (32, 9.0),
                (13, 10.0), // fourth corner
                (23, 11.0),
                (33, 12.0),
                (39, 13.0), // thickness
                (210, 14.0), // extrusion
                (220, 15.0),
                (230, 16.0)
            );
            Assert.Equal(new DxfPoint(1, 2, 3), solid.FirstCorner);
            Assert.Equal(new DxfPoint(4, 5, 6), solid.SecondCorner);
            Assert.Equal(new DxfPoint(7, 8, 9), solid.ThirdCorner);
            Assert.Equal(new DxfPoint(10, 11, 12), solid.FourthCorner);
            Assert.Equal(13.0, solid.Thickness);
            Assert.Equal(new DxfVector(14, 15, 16), solid.ExtrusionDirection);
        }

        [Fact]
        public void ReadLwPolylineWithOptionalValuesTest()
        {
            var lwpolyline = (DxfLwPolyline)Entity("LWPOLYLINE",
                (43, 43.0), // constant width
                (90, 4), // vertex count
                (10, 2.0), // vertex 1
                (20, 0.0),
                (42, 0.7), //          bulge
                (10, 1.0), // vertex 2
                (20, 2.5),
                (42, 0.0), //          bulge
                (10, -1.0), // vertex 3
                (20, 2.5),
                (42, 0.7), //          bulge
                (10, -2.0), // vertex 4
                (20, 0.0),
                (42, 0.0) //           bulge
            );
            Assert.Equal(43.0, lwpolyline.ConstantWidth);
            Assert.Equal(4, lwpolyline.Vertices.Count);

            Assert.Equal(2.0, lwpolyline.Vertices[0].X);
            Assert.Equal(0.0, lwpolyline.Vertices[0].Y);
            Assert.Equal(0.7, lwpolyline.Vertices[0].Bulge);

            Assert.Equal(1.0, lwpolyline.Vertices[1].X);
            Assert.Equal(2.5, lwpolyline.Vertices[1].Y);
            Assert.Equal(0.0, lwpolyline.Vertices[1].Bulge);

            Assert.Equal(-1.0, lwpolyline.Vertices[2].X);
            Assert.Equal(2.5, lwpolyline.Vertices[2].Y);
            Assert.Equal(0.7, lwpolyline.Vertices[2].Bulge);

            Assert.Equal(-2.0, lwpolyline.Vertices[3].X);
            Assert.Equal(0.0, lwpolyline.Vertices[3].Y);
            Assert.Equal(0.0, lwpolyline.Vertices[3].Bulge);
        }

        [Fact]
        public void ReadAttributeTest()
        {
            var att = (DxfAttribute)Entity("ATTRIB",
                (0, "MTEXT"),
                (1, "mtext-value")
            );
            Assert.Equal(att, att.MText.Owner);
            Assert.Equal("mtext-value", att.MText.Text);
        }

        [Fact]
        public void ReadAttributeDefinitionTest()
        {
            var attdef = (DxfAttributeDefinition)Entity("ATTDEF",
                (0, "MTEXT"),
                (1, "mtext-value")
            );
            Assert.Equal(attdef, attdef.MText.Owner);
            Assert.Equal("mtext-value", attdef.MText.Text);
        }

        [Fact]
        public void ReadImageWithImageDefinitionAndReactorTest()
        {
            var file = Parse(
                // entities
                (0, "SECTION"),
                (2, "ENTITIES"),
                (0, "IMAGE"),
                (340, "FFFF0340"), // points to the image definition
                (360, "FFFF0360"), // points to the image definition reactor
                (0, "ENDSEC"),
                // objects
                (0, "SECTION"),
                (2, "OBJECTS"),
                (0, "IMAGEDEF"),
                (5, "FFFF0340"), // from IMAGE.340 above
                (1, "image-def-file-path"),
                (0, "IMAGEDEF_REACTOR"),
                (5, "FFFF0360"), // from IMAGE.360 above
                (0, "ENDSEC"),
                (0, "EOF")
            );
            var image = (DxfImage)file.Entities.Single();
            Assert.Equal("image-def-file-path", image.ImageDefinition.FilePath);
            Assert.Equal(image, image.ImageDefinitionReactor.Owner);
        }

        [Fact]
        public void EnforceMinimumVertexCountOnLeadersTest()
        {
            // need at least 2 vertices
            Assert.Throws<InvalidOperationException>(() => new DxfLeader(new[] { DxfPoint.Origin }));

            var leader = new DxfLeader(new[] { DxfPoint.Origin, DxfPoint.Origin }); // but this should be good
            Assert.Equal(2, leader.Vertices.Count);
            leader.Vertices.Add(DxfPoint.Origin);
            leader.Vertices.RemoveAt(0); // should be allowed because there will be 2 left
            Assert.Throws<InvalidOperationException>(() => leader.Vertices.RemoveAt(0)); // throws because there is only 1 left
        }

        [Fact]
        public void ReadLeaderWithTooFewVerticesTest()
        {
            // a leader with 0 vertices can't be created manually, but it can be read from disk
            var leader = (DxfLeader)Entity("LEADER");
            Assert.Equal(0, leader.Vertices.Count);
            leader.Vertices.Add(DxfPoint.Origin); // this is fine, even though we're still under the minimum
            Assert.Equal(1, leader.Vertices.Count);
            leader.Vertices.Add(DxfPoint.Origin); // now we're up to 2
            Assert.Equal(2, leader.Vertices.Count);
            leader.Vertices.Add(DxfPoint.Origin); // now we're up to 3
            leader.Vertices.RemoveAt(0); // should be fine because we still have 2
            Assert.Equal(2, leader.Vertices.Count);
        }

        [Fact]
        public void ReadLeaderWithIncompleteVerticesTest()
        {
            var leader = (DxfLeader)Entity("LEADER",
                (76, 100), // not really 100 vertices
                (20, 99.0), // specifies a point before one has been started; discarded
                (10, 1.0), // only specifies X
                (10, 2.0), // only specifies XY
                (20, 3.0),
                (10, 4.0), // only specifies XZ
                (30, 5.0),
                (10, 6.0), // specifies XYZ
                (20, 7.0),
                (30, 8.0),
                (10, 9.0), // re-specifies Z; overwritten
                (20, 10.0),
                (30, 11.0),
                (30, 12.0)
            );
            AssertArrayEqual(new[]
            {
                new DxfPoint(1.0, 0.0, 0.0),
                new DxfPoint(2.0, 3.0, 0.0),
                new DxfPoint(4.0, 0.0, 5.0),
                new DxfPoint(6.0, 7.0, 8.0),
                new DxfPoint(9.0, 10.0, 12.0),
            }, leader.Vertices.ToArray());
        }

        [Fact]
        public void EnforceMinimumVertexCountOnPolylineTest()
        {
            // need at least 2 vertices
            Assert.Throws<InvalidOperationException>(() => new DxfPolyline(new[] { new DxfVertex() }));

            var poly = new DxfPolyline(new[] { new DxfVertex(), new DxfVertex() }); // but this should be good
            Assert.Equal(2, poly.Vertices.Count);
            poly.Vertices.Add(new DxfVertex());
            poly.Vertices.RemoveAt(0); // should be allowed because there will be 2 left
            Assert.Throws<InvalidOperationException>(() => poly.Vertices.RemoveAt(0)); // throws because there is only 1 left
        }

        [Fact]
        public void EnsurePolylineSeqendIsAlwaysPresentTest()
        {
            Assert.NotNull(new DxfPolyline().Seqend); // default .ctor
            Assert.NotNull(new DxfPolyline(new DxfVertex[] { new DxfVertex(), new DxfVertex() }).Seqend); // IEnumerable<DxfVertex> .ctor
        }

        [Fact]
        public void ReadPolylineWithTooFewVerticesTest()
        {
            // a polyline with 0 vertices can't be created manually, but it can be read from disk
            var poly = (DxfPolyline)Entity("POLYLINE");
            Assert.Equal(0, poly.Vertices.Count);
            poly.Vertices.Add(new DxfVertex()); // this is fine, even though we're still under the minimum
            Assert.Equal(1, poly.Vertices.Count);
            poly.Vertices.Add(new DxfVertex()); // now we're up to 2
            Assert.Equal(2, poly.Vertices.Count);
            poly.Vertices.Add(new DxfVertex()); // now we're up to 3
            poly.Vertices.RemoveAt(0); // should be fine because we still have 2
            Assert.Equal(2, poly.Vertices.Count);
        }

        [Fact]
        public void ReadSplineWithWeightsTest()
        {
            var spline = (DxfSpline)Entity("SPLINE",
                (73, 2), // point count
                (41, 11.0), // weights
                (41, 22.0),
                (10, 1.1), // point 1
                (20, 1.2),
                (30, 1.3),
                (10, 2.1), // point 2
                (20, 2.2),
                (30, 2.3)
            );
            Assert.Equal(2, spline.ControlPoints.Count);
            Assert.Equal(new DxfPoint(1.1, 1.2, 1.3), spline.ControlPoints[0].Point);
            Assert.Equal(11.0, spline.ControlPoints[0].Weight);
            Assert.Equal(new DxfPoint(2.1, 2.2, 2.3), spline.ControlPoints[1].Point);
            Assert.Equal(22.0, spline.ControlPoints[1].Weight);
        }

        [Fact]
        public void ReadSplineWithoutWeightsTest()
        {
            var spline = (DxfSpline)Entity("SPLINE",
                (73, 2), // point count
                (10, 1.1), // point 1
                (20, 1.2),
                (30, 1.3),
                (10, 2.1), // point 2
                (20, 2.2),
                (30, 2.3)
            );
            Assert.Equal(2, spline.ControlPoints.Count);
            Assert.Equal(new DxfPoint(1.1, 1.2, 1.3), spline.ControlPoints[0].Point);
            Assert.Equal(1.0, spline.ControlPoints[0].Weight);
            Assert.Equal(new DxfPoint(2.1, 2.2, 2.3), spline.ControlPoints[1].Point);
            Assert.Equal(1.0, spline.ControlPoints[1].Weight);
        }

        [Fact]
        public void WriteDefaultLineTest()
        {
            EnsureFileContainsEntity(new DxfLine(),
                (0, "LINE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbLine"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (11, 0.0),
                (21, 0.0),
                (31, 0.0)
            );
        }

        [Fact]
        public void WriteDefaultCircleTest()
        {
            EnsureFileContainsEntity(new DxfCircle(),
                (0, "CIRCLE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbCircle"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (40, 0.0)
            );
        }

        [Fact]
        public void WriteDefaultArcTest()
        {
            EnsureFileContainsEntity(new DxfArc(),
                (0, "ARC"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbCircle"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (40, 0.0),
                (100, "AcDbArc"),
                (50, 0.0),
                (51, 360.0)
            );
        }

        [Fact]
        public void WriteDefaultEllipseTest()
        {
            EnsureFileContainsEntity(new DxfEllipse(),
                DxfAcadVersion.R13,
                (0, "ELLIPSE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbEllipse"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (11, 1.0),
                (21, 0.0),
                (31, 0.0),
                (40, 1.0),
                (41, 0.0),
                (42, Math.PI * 2.0)
            );
        }

        [Fact]
        public void WriteDefaultTextTest()
        {
            EnsureFileContainsEntity(new DxfText(),
                (0, "TEXT"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbText"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (40, 1.0),
                (1, ""),
                (100, "AcDbText")
            );
        }

        [Fact]
        public void WriteDefaultPolylineTest()
        {
            EnsureFileContainsEntity(new DxfPolyline(),
                (0, "POLYLINE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDb2dPolyline"),
                (66, 1),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (0, "SEQEND"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0")
            );
        }

        [Fact]
        public void WriteDefaultSolidTest()
        {
            EnsureFileContainsEntity(new DxfSolid(),
                (0, "SOLID"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbTrace"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (11, 0.0),
                (21, 0.0),
                (31, 0.0),
                (12, 0.0),
                (22, 0.0),
                (32, 0.0),
                (13, 0.0),
                (23, 0.0),
                (33, 0.0)
            );
        }

        [Fact]
        public void WriteLineTest()
        {
            EnsureFileContainsEntity(new DxfLine(new DxfPoint(1, 2, 3), new DxfPoint(4, 5, 6))
                {
                    Color = DxfColor.FromIndex(7),
                    Layer = "bar",
                    Thickness = 7,
                    ExtrusionDirection = new DxfVector(8, 9, 10)
                },
                (0, "LINE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "bar"),
                (62, 7),
                (100, "AcDbLine"),
                (39, 7.0),
                (10, 1.0),
                (20, 2.0),
                (30, 3.0),
                (11, 4.0),
                (21, 5.0),
                (31, 6.0),
                (210, 8.0),
                (220, 9.0),
                (230, 10.0)
            );
        }

        [Fact]
        public void ReadDimensionTest()
        {
            var dimension = (DxfAlignedDimension)Entity("DIMENSION",
                (1, "text"),
                (10, 330.25),
                (20, 1310.0),
                (13, 330.25),
                (23, 1282.0),
                (14, 319.75),
                (24, 1282.0),
                (70, 1)
            );
            Assert.Equal(new DxfPoint(330.25, 1310.0, 0.0), dimension.DefinitionPoint1);
            Assert.Equal(new DxfPoint(330.25, 1282, 0.0), dimension.DefinitionPoint2);
            Assert.Equal(new DxfPoint(319.75, 1282, 0.0), dimension.DefinitionPoint3);
            Assert.Equal("text", dimension.Text);
        }

        [Fact]
        public void WriteDimensionTest()
        {
            EnsureFileContainsEntity(
                new DxfAlignedDimension()
                {
                    Color = DxfColor.FromIndex(7),
                    DefinitionPoint1 = new DxfPoint(330.25, 1310.0, 330.25),
                    DefinitionPoint2 = new DxfPoint(330.25, 1282.0, 0.0),
                    DefinitionPoint3 = new DxfPoint(319.75, 1282.0, 0.0),
                    Layer = "bar",
                    Text = "text"
                },
                (0, "DIMENSION"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "bar"),
                (62, 7),
                (100, "AcDbDimension"),
                (10, 330.25),
                (20, 1310.0),
                (30, 330.25),
                (11, 0.0),
                (21, 0.0),
                (31, 0.0),
                (70, 1),
                (1, "text"),
                (3, "STANDARD"),
                (100, "AcDbAlignedDimension"),
                (13, 330.25),
                (23, 1282.0),
                (33, 0.0),
                (14, 319.75),
                (24, 1282.0),
                (34, 0.0)
            );
        }

        [Fact]
        public void ReadAlignedDimensionWithoutBaselineTest()
        {
            var dim = (DxfAlignedDimension)Entity("DIMENSION",
                (100, "AcDbAlignedDimension")
            );
            Assert.False(dim.IsBaselineAndContinue);
            Assert.Equal(new DxfPoint(0.0, 0.0, 0.0), dim.InsertionPoint);
        }

        [Fact]
        public void WriteAlignedDimensionWithoutBaselineTest()
        {
            EnsureFileDoesNotContainWithEntity(
                new DxfAlignedDimension()
                {
                    InsertionPoint = new DxfPoint(1.0, 2.0, 3.0), // won't be written because IsBaselineAndContinue isn't set
                },
                DxfAcadVersion.R14,
                (12, 1.0),
                (22, 2.0),
                (32, 3.0)
            );
        }

        [Fact]
        public void ReadAlignedDimensionWithBaselineTest()
        {
            var dim = (DxfAlignedDimension)Entity("DIMENSION",
                (100, "AcDbAlignedDimension"),
                (12, 1.0),
                (22, 2.0),
                (32, 3.0)
            );
            Assert.True(dim.IsBaselineAndContinue);
            Assert.Equal(new DxfPoint(1.0, 2.0, 3.0), dim.InsertionPoint);
        }

        [Fact]
        public void WriteAlignedDimensionWithBaselineTest()
        {
            EnsureFileContainsEntity(
                new DxfAlignedDimension()
                {
                    IsBaselineAndContinue = true,
                    InsertionPoint = new DxfPoint(1.0, 2.0, 3.0),
                },
                DxfAcadVersion.R14,
                (12, 1.0),
                (22, 2.0),
                (32, 3.0)
            );
        }

        [Fact]
        public void ReadAngularTwoLineDimensionTest()
        {
            var dim = (DxfAngularTwoLineDimension)Entity("DIMENSION",
                (100, "AcDbDimension"),
                (10, 1.0), // second extension line p1
                (20, 2.0),
                (30, 3.0),
                (11, 4.0), // text mid point
                (21, 5.0),
                (31, 6.0),
                (70, 130), // angular + is at user defined
                (100, "AcDb2LineAngularDimension"),
                (13, 7.0), // first extension line p1
                (23, 8.0),
                (33, 9.0),
                (14, 10.0), // first extension line p2
                (24, 11.0),
                (34, 12.0),
                (15, 13.0), // second extension line p2
                (25, 14.0),
                (35, 15.0),
                (16, 16.0), // dimension line arc location
                (26, 17.0),
                (36, 18.0)
            );
            Assert.True(dim.IsAtUserDefinedLocation);
            Assert.Equal(new DxfPoint(1.0, 2.0, 3.0), dim.SecondExtensionLineP1);
            Assert.Equal(new DxfPoint(4.0, 5.0, 6.0), dim.TextMidPoint);
            Assert.Equal(new DxfPoint(7.0, 8.0, 9.0), dim.FirstExtensionLineP1);
            Assert.Equal(new DxfPoint(10.0, 11.0, 12.0), dim.FirstExtensionLineP2);
            Assert.Equal(new DxfPoint(13.0, 14.0, 15.0), dim.SecondExtensionLineP2);
            Assert.Equal(new DxfPoint(16.0, 17.0, 18.0), dim.DimensionLineArcLocation);
        }

        [Fact]
        public void WriteDimensionWithTrailingXDataTest()
        {
            var dim = new DxfAlignedDimension();
            dim.XData.Add("ACAD",
                new DxfXDataApplicationItemCollection(
                    new DxfXDataString("DSTYLE"),
                    new DxfXDataItemList(
                        new DxfXDataItem[]
                        {
                            new DxfXDataInteger(271),
                            new DxfXDataInteger(9),
                        })
                ));
            // xdata shouldn't be in the generic AcDbDimension section...
            EnsureFileDoesNotContainWithEntity(dim,
                DxfAcadVersion.R14,
                (1002, "}"),
                (100, "AcDbAlignedDimension")
            );
            // ...but _should_ be in the type-specific one at the end of the entity
            EnsureFileContainsEntity(dim,
                DxfAcadVersion.R14,
                (1001, "ACAD"),
                (1000, "DSTYLE"),
                (1002, "{"),
                (1070, 271),
                (1070, 9),
                (1002, "}"),
                (0, "ENDSEC") // the trailing 0/ENDSEC ensures the XData is the last thing written
            );
        }

        [Fact]
        public void WriteDimensionWithStyleDifferenceXData()
        {
            var file = new DxfFile();
            var standardDimStyle = file.DimensionStyles.Single(ds => ds.Name == "STANDARD");
            var customDimStyle = new DxfDimStyle()
            {
                DimensioningSuffix = "some suffix"
            };
            var dim = new DxfAlignedDimension();
            dim.XData["ACAD"] = DxfDimStyle.GenerateStyleDifferenceAsXData(standardDimStyle, customDimStyle);
            EnsureFileContainsEntity(dim,
                DxfAcadVersion.R14,
                (1001, "ACAD"),
                (1000, "DSTYLE"),
                (1002, "{"),
                (1070, 3),
                (1000, "some suffix"),
                (1002, "}"),
                (0, "ENDSEC") // the trailing 0/ENDSEC ensures the XData is the last thing written
            );
        }

        [Fact]
        public void SuperClassXDataIsWrittenTest()
        {
            // DxfCircle is a super class of DxfArc; ensure its XData is written after the entity
            var circle = new DxfCircle();
            circle.XData["appname"] = new DxfXDataApplicationItemCollection(new DxfXDataReal(1.0));
            EnsureFileContainsEntity(circle,
                DxfAcadVersion.R14,
                (1001, "appname"),
                (1040, 1.0),
                (0, "ENDSEC") // the trailing 0/ENDSEC ensures the XData is the last thing written
            );
        }

        [Fact]
        public void SubClassXDataIsOnlyWrittenOnceTest()
        {
            // DxfArc is a sub class of DxfCircle; ensure it's XData is only written once at the very end of the entity
            var arc = new DxfArc();
            arc.XData["appname"] = new DxfXDataApplicationItemCollection(new DxfXDataReal(1.0));
            // xdata shouldn't be in the AcDbCircle section...
            EnsureFileDoesNotContainWithEntity(arc,
                DxfAcadVersion.R14,
                (1001, "appname"),
                (1040, 1.0),
                (100, "AcDbArc")
            );
            // ...but _should_ be at the very end of AcDbArc
            EnsureFileContainsEntity(arc,
                DxfAcadVersion.R14,
                (1001, "appname"),
                (1040, 1.0),
                (0, "ENDSEC") // the trailing 0/ENDSEC ensures the XData is the last thing written
            );
        }

        [Fact]
        public void ReadDimensionWithXDataTest()
        {
            var dimension = (DxfAlignedDimension)Entity("DIMENSION",
                (100, "AcDbAlignedDimension"),
                (1001, "ACAD"),
                (1000, "some xdata string")
            );
            var xdataPair = dimension.XData.Single();
            Assert.Equal("ACAD", xdataPair.Key);
            Assert.Equal("some xdata string", ((DxfXDataString)xdataPair.Value.Single()).Value);
        }

        [Fact]
        public void WriteVertexWithIdentifierTest()
        {
            // non-zero identifiers are written
            var vertex = new DxfVertex()
            {
                Identifier = 42
            };
            var pairs = vertex.GetValuePairs(DxfAcadVersion.R2010, outputHandles: false);
            Assert.Contains(pairs, pair => pair.Code == 91 && pair.IntegerValue == 42);
        }

        [Fact]
        public void WriteVertexWithoutIdentifierTest()
        {
            // zero identifiers are not written
            var vertex = new DxfVertex()
            {
                Identifier = 0
            };
            var pairs = vertex.GetValuePairs(DxfAcadVersion.R2010, outputHandles: false);
            Assert.DoesNotContain(pairs, pair => pair.Code == 91);
        }

        [Fact]
        public void Write2DPolylineTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000; // owner handles only present on R2000+
            var poly = new DxfPolyline();
            poly.Vertices.Add(new DxfVertex());
            poly.Vertices.Add(new DxfVertex());
            file.Entities.Add(poly);
            VerifyFileContains(file,
                DxfSectionType.Entities,
                (0, "SECTION"),
                (2, "ENTITIES"),
                (0, "POLYLINE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (370, 0),
                (100, "AcDb2dPolyline"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (0, "VERTEX"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (370, 0),
                (100, "AcDbVertex"),
                (100, "AcDb2dVertex"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (70, 0),
                (50, 0.0),
                (0, "VERTEX"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (370, 0),
                (100, "AcDbVertex"),
                (100, "AcDb2dVertex"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (70, 0),
                (50, 0.0),
                (0, "SEQEND"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (370, 0),
                (0, "ENDSEC")
            );
        }

        [Fact]
        public void Write3DPolylineTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000; // owner handles only present on R2000+
            var poly = new DxfPolyline();
            poly.Vertices.Add(new DxfVertex());
            poly.Vertices.Add(new DxfVertex());
            poly.Is3DPolyline = true;
            file.Entities.Add(poly);
            VerifyFileContains(file,
                DxfSectionType.Entities,
                (0, "SECTION"),
                (2, "ENTITIES"),
                (0, "POLYLINE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (370, 0),
                (100, "AcDb3dPolyline"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (70, 8),
                (0, "VERTEX"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (370, 0),
                (100, "AcDbVertex"),
                (100, "AcDb3dPolylineVertex"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (70, 32),
                (50, 0.0),
                (0, "VERTEX"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (370, 0),
                (100, "AcDbVertex"),
                (100, "AcDb3dPolylineVertex"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (70, 32),
                (50, 0.0),
                (0, "SEQEND"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (370, 0),
                (0, "ENDSEC")
            );
        }

        [Fact]
        public void WriteLwPolylineWithOptionalValuesTest()
        {
            var lwpolyline = new DxfLwPolyline();
            lwpolyline.Vertices.Add(new DxfLwPolylineVertex() { X = 2.0, Y = 0.0, Bulge = 0.7 });
            lwpolyline.Vertices.Add(new DxfLwPolylineVertex() { X = 1.0, Y = 2.5 });
            lwpolyline.Vertices.Add(new DxfLwPolylineVertex() { X = -1.0, Y = 2.5, Bulge = 0.7 });
            lwpolyline.Vertices.Add(new DxfLwPolylineVertex() { X = -2.0, Y = 0.0 });
            EnsureFileContainsEntity(lwpolyline,
                DxfAcadVersion.R14,
                (0, "LWPOLYLINE"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbPolyline"),
                (90, 4),
                (70, 0),
                (10, 2.0),
                (20, 0.0),
                (42, 0.7),
                (10, 1.0),
                (20, 2.5),
                (10, -1.0),
                (20, 2.5),
                (42, 0.7),
                (10, -2.0),
                (20, 0.0)
            );
        }

        [Fact]
        public void WriteAttributeTest()
        {
            var att = new DxfAttribute();
            att.MText = new DxfMText() { Text = "mtext-value" };
            EnsureFileContainsEntity(att,
                DxfAcadVersion.R13,
                (0, "ATTRIB")
            );
            EnsureFileContainsEntity(att,
                DxfAcadVersion.R13,
                (0, "MTEXT")
            );
        }

        [Fact]
        public void WriteSplineWithWeightsTest()
        {
            var spline = new DxfSpline();
            spline.ControlPoints.Add(new DxfControlPoint(new DxfPoint(1.1, 1.2, 1.3), 11.0));
            spline.ControlPoints.Add(new DxfControlPoint(new DxfPoint(2.1, 2.2, 2.3), 22.0));

            // control points
            EnsureFileContainsEntity(spline,
                DxfAcadVersion.R13,
                (10, 1.1),
                (20, 1.2),
                (30, 1.3),
                (10, 2.1),
                (20, 2.2),
                (30, 2.3)
            );

            // weights
            EnsureFileContainsEntity(spline,
                DxfAcadVersion.R13,
                (41, 11.0),
                (41, 22.0)
            );
        }

        [Fact]
        public void WriteSplineWithoutWeightsTest()
        {
            var spline = new DxfSpline();
            spline.ControlPoints.Add(new DxfControlPoint(new DxfPoint(1.1, 1.2, 1.3), 1.0));
            spline.ControlPoints.Add(new DxfControlPoint(new DxfPoint(2.1, 2.2, 2.3), 1.0));

            // control points
            EnsureFileContainsEntity(spline,
                DxfAcadVersion.R13,
                (10, 1.1),
                (20, 1.2),
                (30, 1.3),
                (10, 2.1),
                (20, 2.2),
                (30, 2.3)
            );

            // weights
            EnsureFileDoesNotContainWithEntity(spline,
                DxfAcadVersion.R13,
                (41, 1.0),
                (41, 1.0)
            );
        }

        [Fact]
        public void ReadBlockTest()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "BLOCKS"),
                    //
                    (0, "BLOCK"),
                    (2, "block 1"),
                    (10, 1.0),
                    (20, 2.0),
                    (30, 3.0),
                        (0, "LINE"),
                        (10, 10.0),
                        (20, 20.0),
                        (30, 30.0),
                        (11, 11.0),
                        (21, 21.0),
                        (31, 31.0),
                    (0, "ENDBLK"),
                    //
                    (0, "BLOCK"),
                    (2, "block 2"),
                        (0, "CIRCLE"),
                        (40, 40.0),
                        (0, "ARC"),
                        (40, 41.0),
                    (0, "ENDBLK"),
                (0, "ENDSEC"),
                (0, "EOF")
            );

            // 2 blocks
            Assert.Equal(2, file.Blocks.Count);

            // first block
            var first = file.Blocks[0];
            Assert.Equal("block 1", first.Name);
            Assert.Equal(new DxfPoint(1, 2, 3), first.BasePoint);
            Assert.Equal(1, first.Entities.Count);
            var entity = first.Entities.First();
            Assert.Equal(DxfEntityType.Line, entity.EntityType);
            var line = (DxfLine)entity;
            Assert.Equal(new DxfPoint(10, 20, 30), line.P1);
            Assert.Equal(new DxfPoint(11, 21, 31), line.P2);

            // second block
            var second = file.Blocks[1];
            Assert.Equal("block 2", second.Name);
            Assert.Equal(2, second.Entities.Count);
            Assert.Equal(DxfEntityType.Circle, second.Entities[0].EntityType);
            Assert.Equal(40.0, ((DxfCircle)second.Entities[0]).Radius);
            Assert.Equal(DxfEntityType.Arc, second.Entities[1].EntityType);
            Assert.Equal(41.0, ((DxfArc)second.Entities[1]).Radius);
        }

        [Fact]
        public void WriteVersionSpecificEntities()
        {
            var file = new DxfFile();
            file.Entities.Add(new DxfProxyEntity());

            file.Header.Version = DxfAcadVersion.R14;
            VerifyFileContains(file,
                DxfSectionType.Entities,
                (0, "ACAD_PROXY_ENTITY")
            );

            file.Header.Version = DxfAcadVersion.R13;
            VerifyFileDoesNotContain(file,
                DxfSectionType.Entities,
                (0, "ACAD_PROXY_ENTITY")
            );
        }

        [Fact]
        public void WriteVersionSpecificEntityProperties()
        {
            var file = new DxfFile();
            file.Entities.Add(new DxfLeader()
            {
                AnnotationOffset = new DxfVector(42.0, 43.0, 44.0),
            });

            // only written on >= R14
            file.Header.Version = DxfAcadVersion.R14;
            VerifyFileContains(file,
                DxfSectionType.Entities,
                (213, 42.0),
                (223, 43.0),
                (233, 44.0)
            );
            file.Header.Version = DxfAcadVersion.R13;
            VerifyFileDoesNotContain(file,
                DxfSectionType.Entities,
                (213, 42.0),
                (223, 43.0),
                (233, 44.0)
            );
        }

        [Fact]
        public void ReadOle2FrameTest()
        {
            var ole = (DxfOle2Frame)Entity("OLE2FRAME",
                (70, 2),
                (3, "Picture (Device Independent Bitmap)"),
                (10, 1.0),
                (20, 2.0),
                (30, 0.0),
                (11, 3.0),
                (21, 4.0),
                (31, 0.0),
                (71, 3),
                (72, 0),
                (90, 5),
                (310, new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A }),
                (1, "OLE")
            );
            Assert.Equal(2, ole.VersionNumber);
            Assert.Equal("Picture (Device Independent Bitmap)", ole.Description);
            Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), ole.UpperLeftCorner);
            Assert.Equal(new DxfPoint(3.0, 4.0, 0.0), ole.LowerRightCorner);
            Assert.Equal(DxfOleObjectType.Static, ole.ObjectType);
            Assert.Equal(DxfTileModeDescriptor.InTiledViewport, ole.TileMode);
            var expected = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A };
            Assert.Equal(expected, ole.Data);
        }

        [Fact]
        public void WriteOle2FrameTest()
        {
            var ole = new DxfOle2Frame()
            {
                VersionNumber = 2,
                Description = "Picture (Device Independent Bitmap)",
                UpperLeftCorner = new DxfPoint(1.0, 2.0, 0.0),
                LowerRightCorner = new DxfPoint(3.0, 4.0, 0.0),
                ObjectType = DxfOleObjectType.Static,
                TileMode = DxfTileModeDescriptor.InTiledViewport,
                Data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A },
            };
            EnsureFileContainsEntity(ole,
                DxfAcadVersion.R14,
                (70, 2),
                (3, "Picture (Device Independent Bitmap)"),
                (10, 1.0),
                (20, 2.0),
                (30, 0.0),
                (11, 3.0),
                (21, 4.0),
                (31, 0.0),
                (71, 3),
                (72, 0),
                (90, 5),
                (310, new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A }),
                (1, "OLE")
            );
        }

        [Fact]
        public void ReadHatchPatternDefinitionTest()
        {
            var hatch = (DxfHatch)Entity("HATCH",
                (77, 1), // IsPatternDoubled, specified before pattern definition
                // pattern definition start
                (78, 2), // line count
                // line 1 start
                (53, 1.0), // angle
                (43, 2.0), // base point
                (44, 3.0),
                (45, 4.0), // offset
                (46, 5.0),
                (79, 2), // dash lengths
                (49, 6.0),
                (49, 7.0),
                // line 2 start
                (53, 8.0),
                (43, 9.0),
                (44, 10.0),
                (45, 11.0),
                (46, 12.0),
                (79, 2),
                (49, 13.0),
                (49, 14.0),
                // PixelSize, specified  after pattern definition lines
                (47, 99.0)
            );
            Assert.True(hatch.IsPatternDoubled); // specified before pattern definition lines
            Assert.Equal(99.0, hatch.PixelSize); // specified after pattern definition lines

            Assert.Equal(2, hatch.PatternDefinitionLines.Count);
            var line1 = hatch.PatternDefinitionLines[0];
            Assert.Equal(1.0, line1.Angle);
            Assert.Equal(2.0, line1.BasePoint.X);
            Assert.Equal(3.0, line1.BasePoint.Y);
            Assert.Equal(4.0, line1.Offset.X);
            Assert.Equal(5.0, line1.Offset.Y);
            AssertArrayEqual(new [] { 6.0, 7.0 }, line1.DashLengths.ToArray());

            var line2 = hatch.PatternDefinitionLines[1];
            Assert.Equal(8.0, line2.Angle);
            Assert.Equal(9.0, line2.BasePoint.X);
            Assert.Equal(10.0, line2.BasePoint.Y);
            Assert.Equal(11.0, line2.Offset.X);
            Assert.Equal(12.0, line2.Offset.Y);
            AssertArrayEqual(new [] { 13.0, 14.0 }, line2.DashLengths.ToArray());
        }

        [Fact]
        public void WriteHatchPatternDefinitionTest()
        {
            var hatch = new DxfHatch();
            hatch.IsPatternDoubled = true; // written before pattern definition lines
            hatch.PixelSize = 99.0; // written after pattern definition lines

            var line1 = new DxfHatch.PatternDefinitionLine();
            line1.Angle = 1.0;
            line1.BasePoint = new DxfPoint(2.0, 3.0, 0.0);
            line1.Offset = new DxfVector(4.0, 5.0, 0.0);
            line1.DashLengths.Add(6.0);
            line1.DashLengths.Add(7.0);

            var line2 = new DxfHatch.PatternDefinitionLine();
            line2.Angle = 8.0;
            line2.BasePoint = new DxfPoint(9.0, 10.0, 0.0);
            line2.Offset = new DxfVector(11.0, 12.0, 0.0);
            line2.DashLengths.Add(13.0);
            line2.DashLengths.Add(14.0);

            hatch.PatternDefinitionLines.Add(line1);
            hatch.PatternDefinitionLines.Add(line2);
            EnsureFileContainsEntity(hatch,
                DxfAcadVersion.R14,
                (77, 1), // IsPatternDoubled, specified before pattern definition
                // pattern definition start
                (78, 2), // line count
                // line 1 start
                (53, 1.0), // angle
                (43, 2.0), // base point
                (44, 3.0),
                (45, 4.0), // offset
                (46, 5.0),
                (79, 2), // dash lengths
                (49, 6.0),
                (49, 7.0),
                // line 2 start
                (53, 8.0),
                (43, 9.0),
                (44, 10.0),
                (45, 11.0),
                (46, 12.0),
                (79, 2),
                (49, 13.0),
                (49, 14.0),
                // PixelSize, specified  after pattern definition lines
                (47, 99.0)
            );
        }

        [Fact]
        public void ReadHatchSeedPointsTest()
        {
            var hatch = (DxfHatch)Entity("HATCH",
                (47, 99.0), // PixelSize, specified before seed points
                (98, 2),
                (10, 1.0), // seed point 1
                (20, 2.0),
                (10, 3.0), // seed point 2
                (20, 4.0),
                (450, 1) // IsGradient, specified after seed points
            );
            Assert.Equal(99.0, hatch.PixelSize); // specified before seed points
            Assert.True(hatch.IsGradient); // specified after seed points

            Assert.Equal(2, hatch.SeedPoints.Count);
            Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), hatch.SeedPoints[0]);
            Assert.Equal(new DxfPoint(3.0, 4.0, 0.0), hatch.SeedPoints[1]);
        }

        [Fact]
        public void WriteHatchSeedPointsTest()
        {
            var hatch = new DxfHatch();
            hatch.PixelSize = 99.0; // written before seed points
            hatch.IsGradient = true; // written after seed points
            hatch.SeedPoints.Add(new DxfPoint(1.0, 2.0, 0.0));
            hatch.SeedPoints.Add(new DxfPoint(3.0, 4.0, 0.0));
            EnsureFileContainsEntity(hatch,
                DxfAcadVersion.R2004,
                (47, 99.0), // PixelSize, specified before seed points
                (98, 2),
                (10, 1.0), // seed point 1
                (20, 2.0),
                (10, 3.0), // seed point 2
                (20, 4.0),
                (450, 1) // IsGradient, specified after seed points
            );
        }

        [Fact]
        public void ReadHatchBoundaryPathDataTest()
        {
            var hatch = (DxfHatch)Entity("HATCH",
                (10, 97.0), // ElevationPoint, specified before boundary paths, shares codes with edge data
                (20, 98.0),
                (30, 99.0),
                (71, 1), // IsAssociative, specified before boundary paths
                (91, 2), // boundary path count
                // first boundary path
                (92, 3), // external polyline boundary type
                (72, 1), // has bulge
                (73, 1), // is closed
                (93, 2), // vertex count
                (10, 1.0), // vertex 1
                (20, 2.0),
                (10, 3.0), // vertex 2 with bulge
                (20, 4.0),
                (42, 5.0),
                (97, 2), // boundary object handles
                (330, "ABC"),
                (330, "DEF"),
                // second boundary path
                (92, 8), // non polyline text box
                (93, 4), // edge count
                // first edge
                (72, 1), // linear edge
                (10, 1.0), // start point
                (20, 2.0),
                (11, 3.0), // end point
                (21, 4.0),
                // second edge
                (72, 2), // circular edge
                (10, 1.0), // center
                (20, 2.0),
                (40, 3.0), // radius
                (50, 4.0), // start angle
                (51, 5.0), // end angle
                (73, 1), // is counter clockwise
                // third edge
                (72, 3), // elliptical edge
                (10, 1.0), // center
                (20, 2.0),
                (11, 3.0), // major axis
                (21, 4.0),
                (40, 5.0), // minor axis ratio
                (50, 6.0), // start angle
                (51, 7.0), // end angle
                (73, 1), // is counter clockwise
                // fourth edge
                (72, 4), // spline edge
                (94, 2), // degree
                (73, 1), // is rational
                (74, 1), // is periodic
                (95, 2), // knot count
                (96, 2), // control point count
                (40, 1.0), // knot values
                (40, 2.0),
                (10, 3.0), // control point 1
                (20, 4.0),
                (10, 5.0), // control point 2
                (20, 6.0),
                (42, 7.0), // weights
                (42, 8.0),
                (97, 2), // fit data count
                (11, 9.0), // first fit point
                (21, 10.0),
                (11, 11.0), // second fit point
                (21, 12.0),
                (12, 13.0), // start tangent
                (22, 14.0),
                (13, 15.0), // end tangent
                (23, 16.0),
                (97, 0), // boundary object handles
                // HatchStyle, specified after boundary paths
                (75, 2)
            );
            Assert.Equal(new DxfPoint(97.0, 98.0, 99.0), hatch.ElevationPoint); // specified before boundary paths, shares codes with edge data
            Assert.True(hatch.IsAssociative); // specified before boundary paths
            Assert.Equal(DxfHatchStyle.EntireArea, hatch.HatchStyle); // specified after boundary paths

            Assert.Equal(2, hatch.BoundaryPaths.Count);
            var polyPath = (DxfHatch.PolylineBoundaryPath)hatch.BoundaryPaths[0];
            Assert.True(polyPath.IsClosed);
            Assert.Equal(2, polyPath.Vertices.Count);
            Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), polyPath.Vertices[0].Location);
            Assert.Equal(0.0, polyPath.Vertices[0].Bulge);
            Assert.Equal(new DxfPoint(3.0, 4.0, 0.0), polyPath.Vertices[1].Location);
            Assert.Equal(5.0, polyPath.Vertices[1].Bulge);
            AssertArrayEqual(new ulong[] { 0xABC, 0xDEF }, polyPath.BoundaryHandles.Select(h => h.Value).ToArray());

            var nonPolyPath = (DxfHatch.NonPolylineBoundaryPath)hatch.BoundaryPaths[1];
            Assert.Equal(DxfHatch.BoundaryPathType.Textbox, nonPolyPath.PathType);
            Assert.Equal(4, nonPolyPath.Edges.Count);

            var linearEdge = (DxfHatch.LineBoundaryPathEdge)nonPolyPath.Edges[0];
            Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), linearEdge.StartPoint);
            Assert.Equal(new DxfPoint(3.0, 4.0, 0.0), linearEdge.EndPoint);

            var circularArcEdge = (DxfHatch.CircularArcBoundaryPathEdge)nonPolyPath.Edges[1];
            Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), circularArcEdge.Center);
            Assert.Equal(3.0, circularArcEdge.Radius);
            Assert.Equal(4.0, circularArcEdge.StartAngle);
            Assert.Equal(5.0, circularArcEdge.EndAngle);
            Assert.True(circularArcEdge.IsCounterClockwise);

            var ellipticArcEdge = (DxfHatch.EllipticArcBoundaryPathEdge)nonPolyPath.Edges[2];
            Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), ellipticArcEdge.Center);
            Assert.Equal(new DxfVector(3.0, 4.0, 0.0), ellipticArcEdge.MajorAxis);
            Assert.Equal(5.0, ellipticArcEdge.MinorAxisRatio);
            Assert.Equal(6.0, ellipticArcEdge.StartAngle);
            Assert.Equal(7.0, ellipticArcEdge.EndAngle);
            Assert.True(ellipticArcEdge.IsCounterClockwise);

            var splineEdge = (DxfHatch.SplineBoundaryPathEdge)nonPolyPath.Edges[3];
            Assert.Equal(2, splineEdge.Degree);
            Assert.True(splineEdge.IsRational);
            Assert.True(splineEdge.IsPeriodic);
            AssertArrayEqual(new[] { 1.0, 2.0 }, splineEdge.Knots.ToArray());
            AssertArrayEqual(new[]
                {
                    new DxfControlPoint(new DxfPoint(3.0, 4.0, 0.0), 7.0),
                    new DxfControlPoint(new DxfPoint(5.0, 6.0, 0.0), 8.0)
                },
                splineEdge.ControlPoints.ToArray());
            AssertArrayEqual(new[]
                {
                    new DxfPoint(9.0, 10.0, 0.0),
                    new DxfPoint(11.0, 12.0, 0.0)
                },
                splineEdge.FitPoints.ToArray());
            Assert.Equal(new DxfVector(13.0, 14.0, 0.0), splineEdge.StartTangent);
            Assert.Equal(new DxfVector(15.0, 16.0, 0.0), splineEdge.EndTangent);
        }

        [Fact]
        public void WriteHatchBoundaryPathDataTest()
        {
            var hatch = new DxfHatch();
            hatch.IsAssociative = true; // written before boundary paths
            hatch.HatchStyle = DxfHatchStyle.EntireArea; // written after boundary paths

            var polyPath = new DxfHatch.PolylineBoundaryPath();
            polyPath.IsClosed = true;
            polyPath.Vertices.Add(new DxfVertex(new DxfPoint(1.0, 2.0, 0.0)));
            polyPath.Vertices.Add(new DxfVertex(new DxfPoint(3.0, 4.0, 0.0)) { Bulge = 5.0 });
            polyPath.BoundaryHandles.Add(new DxfHandle(0xABC));
            polyPath.BoundaryHandles.Add(new DxfHandle(0xDEF));
            hatch.BoundaryPaths.Add(polyPath);

            var nonPolyPath = new DxfHatch.NonPolylineBoundaryPath(DxfHatch.BoundaryPathType.Textbox);
            var linearEdge = new DxfHatch.LineBoundaryPathEdge();
            linearEdge.StartPoint = new DxfPoint(1.0, 2.0, 0.0);
            linearEdge.EndPoint = new DxfPoint(3.0, 4.0, 0.0);
            nonPolyPath.Edges.Add(linearEdge);
            var circularArcEdge = new DxfHatch.CircularArcBoundaryPathEdge();
            circularArcEdge.Center = new DxfPoint(1.0, 2.0, 0.0);
            circularArcEdge.Radius = 3.0;
            circularArcEdge.StartAngle = 4.0;
            circularArcEdge.EndAngle = 5.0;
            circularArcEdge.IsCounterClockwise = true;
            nonPolyPath.Edges.Add(circularArcEdge);
            var ellipticArcEdge = new DxfHatch.EllipticArcBoundaryPathEdge();
            ellipticArcEdge.Center = new DxfPoint(1.0, 2.0, 0.0);
            ellipticArcEdge.MajorAxis = new DxfVector(3.0, 4.0, 0.0);
            ellipticArcEdge.MinorAxisRatio = 5.0;
            ellipticArcEdge.StartAngle = 6.0;
            ellipticArcEdge.EndAngle = 7.0;
            ellipticArcEdge.IsCounterClockwise = true;
            nonPolyPath.Edges.Add(ellipticArcEdge);
            var splineEdge = new DxfHatch.SplineBoundaryPathEdge();
            splineEdge.Degree = 2;
            splineEdge.IsRational = true;
            splineEdge.IsPeriodic = true;
            splineEdge.Knots.Add(1.0);
            splineEdge.Knots.Add(2.0);
            splineEdge.ControlPoints.Add(new DxfControlPoint(new DxfPoint(3.0, 4.0, 0.0), 7.0));
            splineEdge.ControlPoints.Add(new DxfControlPoint(new DxfPoint(5.0, 6.0, 0.0), 8.0));
            splineEdge.FitPoints.Add(new DxfPoint(9.0, 10.0, 0.0));
            splineEdge.FitPoints.Add(new DxfPoint(11.0, 12.0, 0.0));
            splineEdge.StartTangent = new DxfVector(13.0, 14.0, 0.0);
            splineEdge.EndTangent = new DxfVector(15.0, 16.0, 0.0);
            nonPolyPath.Edges.Add(splineEdge);
            hatch.BoundaryPaths.Add(nonPolyPath);

            EnsureFileContainsEntity(hatch,
                DxfAcadVersion.R14,
                (71, 1), // IsAssociative, specified before boundary paths
                (91, 2), // boundary path count
                // first boundary path
                (92, 2), // polyline boundary type
                (72, 1), // has bulge
                (73, 1), // is closed
                (93, 2), // vertex count
                (10, 1.0), // vertex 1
                (20, 2.0),
                (10, 3.0), // vertex 2 with bulge
                (20, 4.0),
                (42, 5.0),
                (97, 2), // boundary object handles
                (330, "ABC"),
                (330, "DEF"),
                // second boundary path
                (92, 8), // non polyline text box
                (93, 4), // edge count
                // first edge
                (72, 1), // linear edge
                (10, 1.0), // start point
                (20, 2.0),
                (11, 3.0), // end point
                (21, 4.0),
                // second edge
                (72, 2), // circular edge
                (10, 1.0), // center
                (20, 2.0),
                (40, 3.0), // radius
                (50, 4.0), // start angle
                (51, 5.0), // end angle
                (73, 1), // is counter clockwise
                // third edge
                (72, 3), // elliptical edge
                (10, 1.0), // center
                (20, 2.0),
                (11, 3.0), // major axis
                (21, 4.0),
                (40, 5.0), // minor axis ratio
                (50, 6.0), // start angle
                (51, 7.0), // end angle
                (73, 1), // is counter clockwise
                // fourth edge
                (72, 4), // spline edge
                (94, 2), // degree
                (73, 1), // is rational
                (74, 1), // is periodic
                (95, 2), // knot count
                (96, 2), // control point count
                (40, 1.0), // knot values
                (40, 2.0),
                (10, 3.0), // control point 1
                (20, 4.0),
                (10, 5.0), // control point 2
                (20, 6.0),
                (42, 7.0), // weights
                (42, 8.0),
                (97, 2), // fit data count
                (11, 9.0), // first fit point
                (21, 10.0),
                (11, 11.0), // second fit point
                (21, 12.0),
                (12, 13.0), // start tangent
                (22, 14.0),
                (13, 15.0), // end tangent
                (23, 16.0),
                (97, 0), // boundary object handles
                // HatchStyle, specified after boundary paths
                (75, 2)
            );
        }

        [Fact]
        public void ReadHatchBoundaryPathWithSourceBoundaryObjectHandlesTest()
        {
            var hatch = (DxfHatch)Entity("HATCH",
                (91, 1), // boundary path count
                (92, 1), // external non-polyline boundary type
                (93, 0), // edge count
                (97, 1), // boundary object handles
                (330, "ABC")
            );
            var externalPath = (DxfHatch.NonPolylineBoundaryPath)hatch.BoundaryPaths.Single();
            var boundaryHandle = externalPath.BoundaryHandles.Single();
            Assert.Equal((ulong)0xABC, boundaryHandle.Value);
        }
    }
}
