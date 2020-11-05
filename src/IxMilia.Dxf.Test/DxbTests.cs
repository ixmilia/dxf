using System.IO;
using System.Linq;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxbTests : AbstractDxfTests
    {
        public static byte[] DxbSentinel => (DxbReader.BinarySentinel + "\r\n").Select(c => (byte)c).Concat(new byte[] { 0x1A, 0x00 }).ToArray();

        [Fact]
        public void ReadDxbTest()
        {
            var data = DxbSentinel.Concat(new byte[]
            {
                // color
                136, // type specifier for new color
                0x01, 0x00, // color index 1

                // line
                0x01, // type specifier
                0x01, 0x00, // P1.X = 0x0001
                0x02, 0x00, // P1.Y = 0x0002
                0x03, 0x00, // P1.Z = 0x0003
                0x04, 0x00, // P2.X = 0x0004
                0x05, 0x00, // P2.Y = 0x0005
                0x06, 0x00, // P2.Z = 0x0006

                0x0                 // null terminator
            }).ToArray();
            using (var stream = new MemoryStream(data))
            {
                var file = DxfFile.Load(stream);
                var line = (DxfLine)file.Entities.Single();
                Assert.Equal(1, line.Color.RawValue);
                Assert.Equal(new DxfPoint(1, 2, 3), line.P1);
                Assert.Equal(new DxfPoint(4, 5, 6), line.P2);
            }
        }

        [Fact]
        public void ReadDxbNoLengthOrPositionStreamTest()
        {
            var data = DxbSentinel.Concat(new byte[]
            {
                // color
                136, // type specifier for new color
                0x01, 0x00, // color index 1

                // line
                0x01, // type specifier
                0x01, 0x00, // P1.X = 0x0001
                0x02, 0x00, // P1.Y = 0x0002
                0x03, 0x00, // P1.Z = 0x0003
                0x04, 0x00, // P2.X = 0x0004
                0x05, 0x00, // P2.Y = 0x0005
                0x06, 0x00, // P2.Z = 0x0006

                0x0 // null terminator
            }).ToArray();
            using (var ms = new MemoryStream(data))
            using (var stream = new StreamWithNoLengthOrPosition(ms))
            {
                var file = DxfFile.Load(stream);
                var line = (DxfLine)file.Entities.Single();
                Assert.Equal(1, line.Color.RawValue);
                Assert.Equal(new DxfPoint(1, 2, 3), line.P1);
                Assert.Equal(new DxfPoint(4, 5, 6), line.P2);
            }
        }

        [Fact]
        public void ReadDxbPolylineTest()
        {
            var data = DxbSentinel.Concat(new byte[]
            {
                19, // polyline
                0x00, 0x00, // is closed = false

                20, // vertex
                0x01, 0x00, // x
                0x02, 0x00, // y

                20, // vertex
                0x03, 0x00, // x
                0x04, 0x00, // y

                17, // seqend

                0x00 // null terminator
            }).ToArray();
            using (var stream = new MemoryStream(data))
            {
                var file = DxfFile.Load(stream);
                var poly = (DxfPolyline)file.Entities.Single();
                Assert.Equal(2, poly.Vertices.Count);
                Assert.Equal(new DxfPoint(1.0, 2.0, 0.0), poly.Vertices[0].Location);
                Assert.Equal(new DxfPoint(3.0, 4.0, 0.0), poly.Vertices[1].Location);
            }
        }

        [Fact]
        public void WriteDxbTest()
        {
            // write file
            var file = new DxfFile();
            file.Entities.Add(new DxfLine(new DxfPoint(1, 2, 3), new DxfPoint(4, 5, 6)));
            var stream = new MemoryStream();
            file.SaveDxb(stream);
            stream.Seek(0, SeekOrigin.Begin);

            // read it back in
            var dxb = DxfFile.Load(stream);
            var line = (DxfLine)dxb.Entities.Single();
            Assert.Equal(new DxfPoint(1, 2, 3), line.P1);
            Assert.Equal(new DxfPoint(4, 5, 6), line.P2);
        }
    }
}
