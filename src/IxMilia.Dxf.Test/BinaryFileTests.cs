using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class BinaryFileTests : AbstractDxfTests
    {
        [Fact]
        public void BinaryReaderTest()
        {
            // this file contains 12 lines
            var file = DxfFile.Load("diamond-bin.dxf");
            Assert.Equal(12, file.Entities.Count);
            Assert.Equal(12, file.Entities.Where(e => e.EntityType == DxfEntityType.Line).Count());
            var first = (DxfLine)file.Entities.First();
            Assert.Equal(new DxfPoint(45, 45, 0), first.P1);
            Assert.Equal(new DxfPoint(45, -45, 0), first.P2);
        }

        [Fact]
        public void BinaryReaderPostR13Test()
        {
            var bytes = new List<byte>();
            void WriteB(params byte[] bs)
            {
                foreach (var b in bs)
                {
                    bytes.Add(b);
                }
            }
            void WriteS(string s, bool addTerminator = true)
            {
                foreach (var c in s)
                {
                    bytes.Add((byte)c);
                }

                if (addTerminator)
                {
                    bytes.Add(0x00);
                }
            }

            WriteS("AutoCAD Binary DXF\r\n", addTerminator: false);
            WriteB(0x1A, 0x00);

            WriteB(0x00, 0x00);
            WriteS("SECTION");
            WriteB(0x02, 0x00);
            WriteS("HEADER");
            WriteB(0x09, 0x00);
            WriteS("$LWDISPLAY");
            WriteB(0x22, 0x01);
            WriteB(0x01);
            WriteB(0x00, 0x00);
            WriteS("ENDSEC");

            WriteB(0x00, 0x00);
            WriteS("EOF");

            using (var ms = new MemoryStream())
            {
                ms.Write(bytes.ToArray(), 0, bytes.Count);
                ms.Seek(0, SeekOrigin.Begin);
                var file = DxfFile.Load(ms);
                Assert.True(file.Header.DisplayLineweightInModelAndLayoutTab);
            }
        }

        [Theory]
        [InlineData(DxfAcadVersion.R12)]
        [InlineData(DxfAcadVersion.R13)]
        public void RoundTripBinaryFileTest(DxfAcadVersion version)
        {
            var file = new DxfFile();
            file.Header.Version = version;
            file.Header.CurrentLayer = "current-layer";
            using (var ms = new MemoryStream())
            {
                file.Save(ms, asText: false);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                var roundTripped = DxfFile.Load(ms);
                Assert.Equal("current-layer", roundTripped.Header.CurrentLayer);
            }
        }

        [Theory]
        [InlineData(DxfAcadVersion.R12, 23)]
        [InlineData(DxfAcadVersion.R13, 24)]
        public void WriteBinaryFileTest(DxfAcadVersion version, int sectionOffset)
        {
            var file = new DxfFile();
            file.Header.Version = version;
            using (var ms = new MemoryStream())
            {
                file.Save(ms, asText: false);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                var buffer = ms.GetBuffer();
                var sentinel = Encoding.ASCII.GetString(buffer, 0, 20);
                Assert.Equal("AutoCAD Binary DXF\r\n", sentinel);
                var sectionText = Encoding.ASCII.GetString(buffer, sectionOffset, 7);
                Assert.Equal("SECTION", sectionText);
            }
        }

        [Fact]
        public void ReadBinaryDxfNoLengthOrPositionStreamTest()
        {
            // this file contains 12 lines
            using (var fs = new FileStream("diamond-bin.dxf", FileMode.Open, FileAccess.Read))
            using (var stream = new StreamWithNoLengthOrPosition(fs))
            {
                var file = DxfFile.Load(stream);
                Assert.Equal(12, file.Entities.Count);
                Assert.Equal(12, file.Entities.Where(e => e.EntityType == DxfEntityType.Line).Count());
            }
        }

        [Fact]
        public void ReadBinaryDxfCodePairOffsetTest()
        {
            using (var fs = new FileStream("diamond-bin.dxf", FileMode.Open, FileAccess.Read))
            {
                using (var binaryReader = new BinaryReader(fs))
                {
                    int readBytes;
                    var firstLine = DxfFile.GetFirstLine(fs, Encoding.ASCII, out readBytes);
                    var dxfReader = DxfFile.GetCodePairReader(firstLine, readBytes, binaryReader, Encoding.ASCII);
                    var codePairs = dxfReader.GetCodePairs().ToList();

                    // verify code pair offsets correspond to file offsets
                    Assert.Equal(100, codePairs.Count);
                    Assert.Equal(22, codePairs.First().Offset);
                    Assert.Equal(805, codePairs.Last().Offset);
                }
            }
        }
    }
}
