using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Objects;
using IxMilia.Dxf.Sections;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfReaderWriterTests : AbstractDxfTests
    {
        private readonly byte[] DxbSentinel = new byte[] { (byte)'A', (byte)'u', (byte)'t', (byte)'o', (byte)'C', (byte)'A', (byte)'D', (byte)' ', (byte)'D', (byte)'X', (byte)'B', (byte)' ', (byte)'1', (byte)'.', (byte)'0', (byte)'\r', (byte)'\n', 0x1A, 0x0 };

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
                Assert.True(file.Header.DisplayLinewieghtInModelAndLayoutTab);
            }
        }

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
        public void ReadEmptyFileTest()
        {
            using (var ms = new MemoryStream())
            {
                var _ = DxfFile.Load(ms);
            }
        }

        [Fact]
        public void ExceptionOnNonDxfFileTest()
        {
            using (var ms = new MemoryStream())
            {
                // made to look like the start of an R14 DWG
                var dwgBytes = new[]
                {
                    'A', 'C', '1', '0', '1', '4',
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };
                foreach (var b in dwgBytes)
                {
                    ms.WriteByte((byte)b);
                }
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                try
                {
                    DxfFile.Load(ms);
                    throw new Exception("Previous call should have thrown.");
                }
                catch (DxfReadException ex)
                {
                    if (ex.Message != "Not a valid DXF file header: `AC1014`.")
                    {
                        throw new Exception("Improper exception", ex);
                    }
                }
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
        public void ReadAsciiDxfNoLengthOrPositionStreamTest()
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            {
                writer.WriteLine(@"
  0
SECTION
  2
ENTITIES
  0
LINE
 10
1
 20
2
 30
3
 11
4
 21
5
 31
6
  0
ENDSEC
  0
EOF
".Trim());
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                using (var stream = new StreamWithNoLengthOrPosition(ms))
                {
                    var file = DxfFile.Load(stream);
                    var line = (DxfLine)file.Entities.Single();
                    Assert.Equal(new DxfPoint(1, 2, 3), line.P1);
                    Assert.Equal(new DxfPoint(4, 5, 6), line.P2);
                }
            }
        }

        [Fact]
        public void ReadAsciiDxfCodePairOffsetTest()
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            {
                writer.WriteLine(@"
  0
SECTION
  2
ENTITIES
  0
LINE
 10
1
 20
2
 30
3
 11
4
 21
5
 31
6
  0
ENDSEC
  0
EOF
".Trim());
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                using (var binaryReader = new BinaryReader(ms))
                {
                    int readBytes;
                    var firstLine = DxfFile.GetFirstLine(ms, Encoding.ASCII, out readBytes);
                    var dxfReader = DxfFile.GetCodePairReader(firstLine, readBytes, binaryReader, Encoding.ASCII);
                    var codePairs = dxfReader.GetCodePairs().ToList();

                    // verify code pair offsets correspond to line numbers
                    Assert.Equal(11, codePairs.Count);
                    Assert.Equal(1, codePairs.First().Offset);
                    Assert.Equal(21, codePairs.Last().Offset);
                }
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

                    // verify code pair offsets correspond to line numbers
                    Assert.Equal(100, codePairs.Count);
                    Assert.Equal(22, codePairs.First().Offset);
                    Assert.Equal(805, codePairs.Last().Offset);
                }
            }
        }

        [Fact]
        public void ReadUnsupportedCodePairsTest()
        {
            var _ = Section("ENTITIES", @"
  0
LINE
5555
unsupported code (5555) treated as string
");
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

        [Fact]
        public void DontWriteBomTest()
        {
            using (var stream = new MemoryStream())
            {
                var file = new DxfFile();
                file.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var buffer = new byte[3];
                stream.Read(buffer, 0, buffer.Length);

                // first three bytes should be "  0", not 0xEF, 0xBB, 0xBF
                Assert.Equal(new byte[] { (byte)' ', (byte)' ', (byte)'0' }, buffer);
            }
        }

        [Fact]
        public void SkipBomTest()
        {
            // UTF-8 representation of byte order mark
            var bom = new byte[]
            {
                0xEF,
                0xBB,
                0xBF,
            };
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                stream.Write(bom, 0, bom.Length);
                writer.Write("0\r\nEOF");
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                var file = DxfFile.Load(stream);
                Assert.Equal(0, file.Layers.Count);
            }
        }

        [Fact]
        public void ReadWithDifferingNewLinesTest()
        {
            var lines = new List<string>()
            {
                "0", "SECTION",
                "2", "ENTITIES",
                "0", "LINE",
                "0", "ENDSEC",
                "0", "EOF"
            };

            // verify reading LF
            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
                {
                    writer.Write(string.Join("\n", lines));
                }

                ms.Seek(0, SeekOrigin.Begin);
                var file = DxfFile.Load(ms);
                Assert.IsType<DxfLine>(file.Entities.Single());
            }

            // verify reading CRLF
            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
                {
                    writer.Write(string.Join("\r\n", lines));
                }

                ms.Seek(0, SeekOrigin.Begin);
                var file = DxfFile.Load(ms);
                Assert.IsType<DxfLine>(file.Entities.Single());
            }
        }

        [Fact]
        public void WriteCarriageReturnNewLineTest()
        {
            using (var ms = new MemoryStream())
            {
                new DxfFile().Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms))
                {
                    var text = reader.ReadToEnd();
                    Assert.Contains("\r\n", text);
                }
            }
        }

        [Fact]
        public void ReadWithExtraTrailingNewlinesTest()
        {
            // file must be created this way to ensure all appropriate newlines are present for parsing
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            {
                writer.Write("0\r\nEOF\r\n\r\n");
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                var file = DxfFile.Load(ms);
            }
        }

        [Fact]
        public void ReadThumbnailTest()
        {
            var file = Section("THUMBNAILIMAGE",
                (90, 3),
                (310, "012345")
            );
            AssertArrayEqual(file.RawThumbnail, new byte[] { 0x01, 0x23, 0x45 });
        }

        [Fact]
        public void WriteThumbnailTestR14()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            file.RawThumbnail = new byte[] { 0x01, 0x23, 0x45 };
            VerifyFileDoesNotContain(file,
                (0, "SECTION"),
                (2, "THUMBNAILIMAGE")
            );
        }

        [Fact]
        public void WriteThumbnailTestR2000()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            file.RawThumbnail = new byte[] { 0x01, 0x23, 0x45 };
            VerifyFileContains(file,
                (0, "SECTION"),
                (2, "THUMBNAILIMAGE"),
                (90, 3),
                (310, "012345"),
                (0, "ENDSEC")
            );
        }

        [Fact]
        public void WriteThumbnailTest_SetThumbnailBitmap()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            var header = DxfThumbnailImageSection.BITMAPFILEHEADER;
            var bitmap = header.Concat(new byte[] { 0x01, 0x23, 0x45 }).ToArray();
            file.SetThumbnailBitmap(bitmap);
            VerifyFileContains(file,
                (0, "SECTION"),
                (2, "THUMBNAILIMAGE"),
                (90, 3),
                (310, "012345"),
                (0, "ENDSEC")
            );
        }

        [Fact]
        public void ReadThumbnailTest_GetThumbnailBitmap()
        {
            var file = Section("THUMBNAILIMAGE",
                (90, 3),
                (310, "012345")
            );
            var expected = new byte[]
            {
                (byte)'B', (byte)'M', // magic number
                0x03, 0x00, 0x00, 0x00, // file length excluding header
                0x00, 0x00, // reserved
                0x00, 0x00, // reserved
                0x36, 0x04, 0x00, 0x00, // bit offset; always 1078
                0x01, 0x23, 0x45 // body
            };
            var bitmap = file.GetThumbnailBitmap();
            AssertArrayEqual(expected, bitmap);
        }

        [Fact]
        public void AssignOwnerHandlesInXDataTest()
        {
            // read a layout with its owner handle also specified in the XDATA
            var file = Parse(
                (0, "SECTION"),
                (2, "HEADER"),
                (9, "$ACADVER"),
                (1, "AC1027"),
                (0, "ENDSEC"),
                (0, "SECTION"),
                (2, "OBJECTS"),
                (0, "DICTIONARY"),
                (5, "BBBBBBBB"),
                (3, "some-layout"),
                (350, "CCCCCCCC"),
                (0, "LAYOUT"),
                (5, "CCCCCCCC"),
                (330, "BBBBBBBB"),
                (102, "{ACAD_REACTORS"),
                (330, "BBBBBBBB"),
                (102, "}"),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            // sanity check to verify that it was read correctly
            var dict = file.Objects.OfType<DxfDictionary>().Single();
            var layout = (DxfLayout)dict["some-layout"];
            Assert.Equal(0xBBBBBBBB, ((IDxfItemInternal)dict).Handle);
            Assert.Equal(0xCCCCCCCC, ((IDxfItemInternal)layout).Handle);

            // re-save the file to a garbage stream to re-assign handles
            using (var ms = new MemoryStream())
            {
                file.Save(ms);
            }

            // verify new handles and owners; note that the assigned handles are unlikely to be 0xBBBBBBBB and 0xCCCCCCCC again
            Assert.True(ReferenceEquals(layout.Owner, dict));
            Assert.NotEqual(0xBBBBBBBB, ((IDxfItemInternal)dict).Handle);
            Assert.NotEqual(0xCCCCCCCC, ((IDxfItemInternal)layout).Handle);
            var dictHandle = ((IDxfItemInternal)dict).Handle;
            Assert.Equal(dictHandle, ((IDxfItemInternal)layout).OwnerHandle);
            var layoutXDataGroups = ((IDxfHasXData)layout).ExtensionDataGroups.Single(g => g.GroupName == "ACAD_REACTORS");
            var ownerCodePair = (DxfCodePair)layoutXDataGroups.Items.Single();
            Assert.Equal(330, ownerCodePair.Code);
            Assert.Equal(DxfCommonConverters.UIntHandle(dictHandle), ownerCodePair.StringValue);
        }

        [Fact]
        public void ReadVersionSpecificClassTest_R13()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "HEADER"),
                (9, "$ACADVER"),
                (1, "AC1012"),
                (0, "ENDSEC"),
                (0, "SECTION"),
                (2, "CLASSES"),
                (0, "<class dxf name>"),
                (1, "CPP_CLASS_NAME"),
                (2, "<application name>"),
                (90, 42),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            Assert.Equal(1, file.Classes.Count);

            var cls = file.Classes.Single();
            Assert.Equal("<class dxf name>", cls.ClassDxfRecordName);
            Assert.Equal("CPP_CLASS_NAME", cls.CppClassName);
            Assert.Equal("<application name>", cls.ApplicationName);
            Assert.Equal(42, cls.ClassVersionNumber);
        }

        [Fact]
        public void ReadVersionSpecificClassTest_R14()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "HEADER"),
                (9, "$ACADVER"),
                (1, "AC1014"),
                (0, "ENDSEC"),
                (0, "SECTION"),
                (2, "CLASSES"),
                (0, "CLASS"),
                (1, "<class dxf name>"),
                (2, "CPP_CLASS_NAME"),
                (3, "<application name>"),
                (90, 42),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            Assert.Equal(1, file.Classes.Count);

            var cls = file.Classes.Single();
            Assert.Equal("<class dxf name>", cls.ClassDxfRecordName);
            Assert.Equal("CPP_CLASS_NAME", cls.CppClassName);
            Assert.Equal("<application name>", cls.ApplicationName);
            Assert.Equal(42, cls.ProxyCapabilities.Value);
        }

        [Fact]
        public void ReadVersionSpecificBlockRecordTest_R2000()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "BLOCK_RECORD"),
                (5, "2"),
                (330, "0"),
                (100, "AcDbSymbolTable"),
                (70, 0),
                (0, "BLOCK_RECORD"),
                (5, "A"),
                (330, "0"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbBlockTableRecord"),
                (2, "<name>"),
                (340, "A1"),
                (310, "010203040506070809"),
                (310, "010203040506070809"),
                (1001, "ACAD"),
                (1000, "DesignCenter Data"),
                (1002, "{"),
                (1070, 0),
                (1070, 1),
                (1070, 2),
                (1002, "}"),
                (0, "ENDTAB")
            );
            var blockRecord = file.BlockRecords.Single();
            Assert.Equal("<name>", blockRecord.Name);
            Assert.Equal(0xA1u, blockRecord.LayoutHandle);

            var xdata = blockRecord.XData;
            Assert.Equal("ACAD", xdata.ApplicationName);

            var namedList = (DxfXDataNamedList)xdata.Items.Single();
            Assert.Equal("DesignCenter Data", namedList.Name);
            Assert.Equal(3, namedList.Items.Count);
            Assert.Equal(0, ((DxfXDataInteger)namedList.Items[0]).Value);
            Assert.Equal(1, ((DxfXDataInteger)namedList.Items[1]).Value);
            Assert.Equal(2, ((DxfXDataInteger)namedList.Items[2]).Value);

            AssertArrayEqual(new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09
            }, blockRecord.BitmapData);
        }

        [Fact]
        public void ReadRealTriple()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "BLOCK_RECORD"),
                (5, "2"),
                (330, "0"),
                (100, "AcDbSymbolTable"),
                (70, 0),
                (0, "BLOCK_RECORD"),
                (5, "A"),
                (330, "0"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbBlockTableRecord"),
                (2, "<name>"),
                (340, "A1"),
                (310, "010203040506070809"),
                (310, "010203040506070809"),
                (1001, "ACAD"),
                (1000, "DesignCenter Data"),
                (1002, "{"),
                (1070, 0),
                (1070, 1),
                (1070, 2),
                (1010, 3.1),
                (1020, 4.2),
                (1030, 5.3),
                (1002, "}"),
                (0, "ENDTAB")
            );
            var blockRecord = file.BlockRecords.Single();
            Assert.Equal("<name>", blockRecord.Name);
            Assert.Equal(0xA1u, blockRecord.LayoutHandle);

            var xdata = blockRecord.XData;
            Assert.Equal("ACAD", xdata.ApplicationName);

            var namedList = (DxfXDataNamedList)xdata.Items.Single();
            Assert.Equal("DesignCenter Data", namedList.Name);

            Assert.Equal(4, namedList.Items.Count);
            Assert.Equal(0, ((DxfXDataInteger)namedList.Items[0]).Value);
            Assert.Equal(1, ((DxfXDataInteger)namedList.Items[1]).Value);
            Assert.Equal(2, ((DxfXDataInteger)namedList.Items[2]).Value);
            Assert.Equal(new DxfPoint(3.1, 4.2, 5.3), ((DxfXData3Reals)namedList.Items[3]).Value);
            AssertArrayEqual(new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09
            }, blockRecord.BitmapData);
        }

        [Fact]
        public void ReadXDataUnexpectedValueTest()
        {
            var file = Section("ENTITIES",
                (0, "LINE"),
                (1001, "group name"),
                (1002, "{"),
                (1011, 1.0), // valid world point
                (1021, 2.0),
                (1031, 3.0),
                (1021, 222.0), // point missing x value; invalid and skipped
                (1031, 333.0),
                (1011, 11.0), // valid world point
                (1021, 22.0),
                (1031, 33.0),
                (1002, "}")
            );
            var line = (DxfLine)file.Entities.Single();
            var xdata = ((IDxfHasXDataHidden)line).XDataHidden;
            var itemList = (DxfXDataItemList)xdata.Items.Single();
            Assert.Equal(2, itemList.Items.Count);
            Assert.Equal(new DxfPoint(1, 2, 3), ((DxfXDataWorldSpacePosition)itemList.Items.First()).Value);
            Assert.Equal(new DxfPoint(11, 22, 33), ((DxfXDataWorldSpacePosition)itemList.Items.Last()).Value);
        }

        [Fact]
        public void WriteVersionSpecificBlockRecordTest_R14()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            var blockRecord = new DxfBlockRecord()
            {
                Name = "<name>",
                XData = new DxfXData("ACAD",
                    new DxfXDataItem[]
                    {
                        new DxfXDataString("DesignCenter Data"),
                        new DxfXDataItemList(
                            new []
                            {
                                new DxfXDataInteger(0),
                                new DxfXDataInteger(1),
                                new DxfXDataInteger(2)
                            })
                    })
            };
            file.BlockRecords.Add(blockRecord);
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "TABLE"),
                (2, "BLOCK_RECORD"),
                (5, "#"),
                (100, "AcDbSymbolTable"),
                (70, 3),
                (0, "BLOCK_RECORD"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbBlockTableRecord"),
                (2, "*MODEL_SPACE"),
                (0, "BLOCK_RECORD"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbBlockTableRecord"),
                (2, "*PAPER_SPACE"),
                (0, "BLOCK_RECORD"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbBlockTableRecord"),
                (2, "<name>"),
                (1001, "ACAD"),
                (1000, "DesignCenter Data"),
                (1002, "{"),
                (1070, 0),
                (1070, 1),
                (1070, 2),
                (1002, "}"),
                (0, "ENDTAB")
            );
        }

        [Fact]
        public void WriteVersionSpecificBlockRecordTest_R2000()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            var blockRecord = new DxfBlockRecord()
            {
                Name = "<name>",
                LayoutHandle = 0x43u,
                XData = new DxfXData("ACAD",
                    new DxfXDataItem[]
                    {
                        new DxfXDataString("DesignCenter Data"),
                        new DxfXDataItemList(
                            new []
                            {
                                new DxfXDataInteger(0),
                                new DxfXDataInteger(1),
                                new DxfXDataInteger(2)
                            })
                    }),
                BitmapData = new byte[]
                {
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09
                }
            };
            file.BlockRecords.Add(blockRecord);
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "TABLE"),
                (2, "BLOCK_RECORD"),
                (5, "#"),
                (100, "AcDbSymbolTable"),
                (70, 3),
                (0, "BLOCK_RECORD"),
                (5, "#"),
                (330, "#"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbBlockTableRecord"),
                (2, "*MODEL_SPACE"),
                (0, "BLOCK_RECORD"),
                (5, "#"),
                (330, "#"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbBlockTableRecord"),
                (2, "*PAPER_SPACE"),
                (0, "BLOCK_RECORD"),
                (5, "#"),
                (330, "#"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbBlockTableRecord"),
                (2, "<name>"),
                (340, "43"),
                (310, "010203040506070809010203040506070809"),
                (1001, "ACAD"),
                (1000, "DesignCenter Data"),
                (1002, "{"),
                (1070, 0),
                (1070, 1),
                (1070, 2),
                (1002, "}"),
                (0, "ENDTAB")
            );
        }

        [Fact]
        public void WriteVersionSpecificClassTest_R13()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R13;
            file.Classes.Add(new DxfClass()
            {
                ClassDxfRecordName = "<class dxf name>",
                CppClassName = "CPP_CLASS_NAME",
                ApplicationName = "<application name>",
                ClassVersionNumber = 42
            });
            VerifyFileContains(file,
                DxfSectionType.Classes,
                (0, "SECTION"),
                (2, "CLASSES"),
                (0, "<class dxf name>"),
                (1, "CPP_CLASS_NAME"),
                (2, "<application name>"),
                (90, 42)
            );
        }

        [Fact]
        public void WriteVersionSpecificClassTest_R14()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            file.Classes.Add(new DxfClass()
            {
                ClassDxfRecordName = "<class dxf name>",
                CppClassName = "CPP_CLASS_NAME",
                ApplicationName = "<application name>",
                ProxyCapabilities = new DxfProxyCapabilities(42)
            });
            VerifyFileContains(file,
                DxfSectionType.Classes,
                (0, "SECTION"),
                (2, "CLASSES"),
                (0, "CLASS"),
                (1, "<class dxf name>"),
                (2, "CPP_CLASS_NAME"),
                (3, "<application name>"),
                (90, 42)
            );
        }

        [Fact]
        public void ReadVersionSpecificBlockTest_R13()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "HEADER"),
                (9, "$ACADVER"),
                (1, "AC1012"),
                (0, "ENDSEC"),
                (0, "SECTION"),
                (2, "BLOCKS"),
                (0, "BLOCK"),
                (5, "42"),
                (100, "AcDbEntity"),
                (8, "<layer>"),
                (100, "AcDbBlockBegin"),
                (2, "<block name>"),
                (70, 0),
                (10, 11.0),
                (20, 22.0),
                (30, 33.0),
                (3, "<block name>"),
                (1, "<xref path>"),
                (0, "POINT"),
                (10, 1.1),
                (20, 2.2),
                (30, 3.3),
                (0, "ENDBLK"),
                (5, "42"),
                (100, "AcDbEntity"),
                (8, "<layer>"),
                (100, "AcDbBlockEnd"),
                (0, "ENDSEC"),
                (0, "EOF")
            );

            var block = file.Blocks.Single();
            Assert.Equal("<block name>", block.Name);
            Assert.Equal(0x42u, ((IDxfItemInternal)block).Handle);
            Assert.Equal("<layer>", block.Layer);
            Assert.Equal(11, block.BasePoint.X);
            Assert.Equal(22, block.BasePoint.Y);
            Assert.Equal(33, block.BasePoint.Z);
            var point = (DxfModelPoint)block.Entities.Single();
            Assert.Equal(1.1, point.Location.X);
            Assert.Equal(2.2, point.Location.Y);
            Assert.Equal(3.3, point.Location.Z);
        }

        [Fact]
        public void ReadVersionSpecificBlockTest_R14()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "HEADER"),
                (9, "$ACADVER"),
                (1, "AC1014"),
                (0, "ENDSEC"),
                (0, "SECTION"),
                (2, "BLOCKS"),
                (0, "BLOCK"),
                (5, "42"),
                (100, "AcDbEntity"),
                (8, "<layer>"),
                (100, "AcDbBlockBegin"),
                (2, "<block name>"),
                (70, 0),
                (10, 11.0),
                (20, 22.0),
                (30, 33.0),
                (3, "<block name>"),
                (1, "<xref>"),
                (0, "POINT"),
                (10, 1.1),
                (20, 2.2),
                (30, 3.3),
                (0, "ENDBLK"),
                (5, "42"),
                (100, "AcDbBlockEnd"),
                (0, "ENDSEC"),
                (0, "EOF")
            );

            var block = file.Blocks.Single();
            Assert.Equal("<block name>", block.Name);
            Assert.Equal("<layer>", block.Layer);
            Assert.Equal("<xref>", block.XrefName);
            Assert.Equal(11, block.BasePoint.X);
            Assert.Equal(22, block.BasePoint.Y);
            Assert.Equal(33, block.BasePoint.Z);
            var point = (DxfModelPoint)block.Entities.Single();
            Assert.Equal(1.1, point.Location.X);
            Assert.Equal(2.2, point.Location.Y);
            Assert.Equal(3.3, point.Location.Z);
        }

        [Fact]
        public void WriteVersionSpecificBlockTest_R13()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R13;
            var block = new DxfBlock();
            block.Name = "<block name>";
            block.Layer = "<layer>";
            block.XrefName = "<xref>";
            block.BasePoint = new DxfPoint(11, 22, 33);
            block.Entities.Add(new DxfModelPoint(new DxfPoint(111, 222, 333)));
            file.Blocks.Add(block);
            VerifyFileContains(file,
                (0, "BLOCK"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "<layer>"),
                (100, "AcDbBlockBegin"),
                (2, "<block name>"),
                (70, 0),
                (10, 11.0),
                (20, 22.0),
                (30, 33.0),
                (3, "<block name>"),
                (1, "<xref>")
            );
            VerifyFileContains(file,
                (10, 111.0),
                (20, 222.0),
                (30, 333.0)
            );
            VerifyFileContains(file,
                (0, "ENDBLK"),
                (5, "#"),
                (100, "AcDbEntity"),
                (8, "<layer>"),
                (100, "AcDbBlockEnd"),
                (0, "ENDSEC")
            );
        }

        [Fact]
        public void ReadBlockWithUnsupportedEntityTest()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "BLOCKS"),
                (0, "BLOCK"),
                (100, "AcDbBlockBegin"),
                (0, "POINT"),
                (10, 1.1),
                (20, 2.2),
                (30, 3.3),
                (0, "NOT_A_SUPPORTED_ENTITY"), // unsupported entity between two supported entities
                (0, "POINT"),
                (10, 4.4),
                (20, 5.5),
                (30, 6.6),
                (0, "ENDBLK"),
                (5, "42"),
                (100, "AcDbBlockEnd"),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            var block = file.Blocks.Single();
            Assert.Equal(2, block.Entities.Count);
            var p1 = (DxfModelPoint)block.Entities.First();
            Assert.Equal(new DxfPoint(1.1, 2.2, 3.3), p1.Location);
            var p2 = (DxfModelPoint)block.Entities.Last();
            Assert.Equal(new DxfPoint(4.4, 5.5, 6.6), p2.Location);
        }

        [Fact]
        public void ReadXDataNamedGroupTest()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "ENTITIES"),
                (0, "DIMENSION"),
                (100, "AcDbAlignedDimension"),
                (1001, "ACAD"),
                (1000, "leading string"),
                (1000, "DSTYLE"),
                (1002, "{"),
                (1070, 54),
                (1000, "some string"),
                (1002, "}"),
                (1070, 42),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            var dim = (DxfAlignedDimension)file.Entities.Single();
            Assert.Equal("ACAD", dim.XData.ApplicationName);
            Assert.Equal(3, dim.XData.Items.Count);

            Assert.Equal("leading string", ((DxfXDataString)dim.XData.Items[0]).Value);

            var namedList = (DxfXDataNamedList)dim.XData.Items[1];
            Assert.Equal("DSTYLE", namedList.Name);
            Assert.Equal(2, namedList.Items.Count);
            Assert.Equal(54, ((DxfXDataInteger)namedList.Items[0]).Value);
            Assert.Equal("some string", ((DxfXDataString)namedList.Items[1]).Value);

            Assert.Equal(42, ((DxfXDataInteger)dim.XData.Items[2]).Value);
        }

        [Fact]
        public void WriteXDataNamedGroupTest()
        {
            var dim = new DxfAlignedDimension();
            dim.XData = new DxfXData("ACAD",
                new[]
                {
                    new DxfXDataNamedList("DSTYLE",
                        new[]
                        {
                            new DxfXDataInteger(271),
                            new DxfXDataInteger(9),
                        })
                });
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            file.Entities.Add(dim);
            VerifyFileContains(file,
                DxfSectionType.Entities,
                (1001, "ACAD"),
                (1000, "DSTYLE"),
                (1002, "{"),
                (1070, 271),
                (1070, 9),
                (1002, "}")
            );
        }

        [Fact]
        public void ReadBlockWithPolylineTest()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "BLOCKS"),
                (0, "BLOCK"),
                (100, "AcDbBlockBegin"),
                (0, "POLYLINE"),
                (0, "VERTEX"),
                (10, 1.1),
                (20, 2.2),
                (30, 3.3),
                (0, "VERTEX"),
                (10, 4.4),
                (20, 5.5),
                (30, 6.6),
                (0, "VERTEX"),
                (10, 7.7),
                (20, 8.8),
                (30, 9.9),
                (0, "VERTEX"),
                (10, 10.0),
                (20, 11.1),
                (30, 12.2),
                (0, "SEQEND"),
                (0, "ENDBLK"),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            var block = file.Blocks.Single();
            var pl = (DxfPolyline)block.Entities.Single();
            Assert.Equal(4, pl.Vertices.Count);
            Assert.Equal(new DxfPoint(1.1, 2.2, 3.3), pl.Vertices[0].Location);
            Assert.Equal(new DxfPoint(4.4, 5.5, 6.6), pl.Vertices[1].Location);
            Assert.Equal(new DxfPoint(7.7, 8.8, 9.9), pl.Vertices[2].Location);
            Assert.Equal(new DxfPoint(10.0, 11.1, 12.2), pl.Vertices[3].Location);
        }

        [Fact]
        public void ReadTableTest()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "TABLES"),
                (0, "TABLE"),
                (2, "STYLE"),
                (5, "1C"),
                (102, "{ACAD_XDICTIONARY"),
                (360, "AAAA"),
                (360, "BBBB"),
                (102, "}"),
                (70, 3),
                (1001, "APP_X"),
                (1040, 42.0),
                (0, "STYLE"),
                (5, "3A"),
                (2, "ENTRY_1"),
                (70, 64),
                (40, 0.4),
                (41, 1.0),
                (50, 0.0),
                (71, 0),
                (42, 0.4),
                (3, "BUFONTS.TXT"),
                (0, "STYLE"),
                (5, "C2"),
                (2, "ENTRY_2"),
                (3, "BUFONTS.TXT"),
                (1001, "APP_1"),
                (1070, 45),
                (1001, "APP_2"),
                (1004, "18A5B3EF2C199A"),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            var styleTable = file.TablesSection.StyleTable;
            Assert.Equal(0x1Cu, styleTable.Handle);
            Assert.Equal(2, styleTable.Items.Count);

            var extendedDataGroup = styleTable.ExtensionDataGroups.Single();
            Assert.Equal("ACAD_XDICTIONARY", extendedDataGroup.GroupName);
            Assert.Equal(2, extendedDataGroup.Items.Count);
            Assert.Equal(new DxfCodePair(360, "AAAA"), extendedDataGroup.Items[0]);
            Assert.Equal(new DxfCodePair(360, "BBBB"), extendedDataGroup.Items[1]);

            var style1 = file.Styles.First();
            Assert.Equal(0x3Au, style1.Handle);
            Assert.Equal("ENTRY_1", style1.Name);
            Assert.Equal(64, style1.StandardFlags);
            Assert.Equal(0.4, style1.TextHeight);
            Assert.Equal(1.0, style1.WidthFactor);
            Assert.Equal(0.0, style1.ObliqueAngle);
            Assert.Equal(0, style1.TextGenerationFlags);
            Assert.Equal(0.4, style1.LastHeightUsed);
            Assert.Equal("BUFONTS.TXT", style1.PrimaryFontFileName);

            var style2 = file.Styles.Skip(1).Single();
            Assert.Equal(0xC2u, style2.Handle);
            Assert.Equal("ENTRY_2", style2.Name);
            Assert.Equal("BUFONTS.TXT", style2.PrimaryFontFileName);
        }

        [Fact]
        public void WriteTableWithoutExtendedDataTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            file.Styles.Add(new DxfStyle());
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "TABLE"),
                (2, "STYLE"),
                (5, "#"),
                (100, "AcDbSymbolTable"),
                (70, 3)
            );
        }

        [Fact]
        public void WriteTableWithExtendedDataTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            file.TablesSection.StyleTable.ExtensionDataGroups.Add(new DxfCodePairGroup("ACAD_XDICTIONARY",
                new IDxfCodePairOrGroup[]
                {
                    new DxfCodePair(360, "AAAA"),
                    new DxfCodePair(360, "BBBB")
                }));
            file.Styles.Add(new DxfStyle());
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "TABLE"),
                (2, "STYLE"),
                (5, "#"),
                (102, "{ACAD_XDICTIONARY"),
                (360, "AAAA"),
                (360, "BBBB"),
                (102, "}"),
                (100, "AcDbSymbolTable"),
                (70, 3)
            );
        }

        [Fact]
        public void WriteVersionSpecificSectionsTest_R12()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R12;
            file.Classes.Add(new DxfClass());

            // no CLASSES section in R12
            VerifyFileDoesNotContain(file,
                (0, "SECTION"),
                (2, "CLASSES")
            );

            // no OBJECTS section in R12
            VerifyFileDoesNotContain(file,
                (0, "SECTION"),
                (2, "OBJECTS")
            );
        }

        [Fact]
        public void WriteVersionSpecificSectionsTest_R13()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R13;
            file.Classes.Add(new DxfClass());

            // CLASSES section added in R13
            VerifyFileContains(file,
                (0, "SECTION"),
                (2, "CLASSES")
            );

            // OBJECTS section added in R13
            VerifyFileContains(file,
                (0, "SECTION"),
                (2, "OBJECTS")
            );
        }

        [Fact]
        public void WriteVersionSpecificBlockRecordTest_R12()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R12;
            file.BlockRecords.Add(new DxfBlockRecord());

            // no BLOCK_RECORD in R12
            VerifyFileDoesNotContain(file,
                (0, "TABLE"),
                (2, "BLOCK_RECORD")
            );
        }

        [Fact]
        public void WriteVersionSpecificBlockRecordTest_R13()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R13;
            file.BlockRecords.Add(new DxfBlockRecord());

            // BLOCK_RECORD added in R13
            VerifyFileContains(file,
                (0, "TABLE"),
                (2, "BLOCK_RECORD")
            );
        }

        [Fact]
        public void Code280ShortInsteadOfCode290BoolTest()
        {
            // the spec says header variables $HIDETEXT, $INTERSECTIONDISPLAY, and $XCLIPFRAME should be code 290
            // bools but some R2010 files encountered in the wild have a code 280 short instead

            // first test code 290 bool
            var file = Section("HEADER",
                (9, "$ACADVER"), (1, "AC1018"),
                (9, "$HIDETEXT"), (290, 1),
                (9, "$INTERSECTIONDISPLAY"), (290, 1),
                (9, "$XCLIPFRAME"), (290, 1)
            );
            Assert.True(file.Header.HideTextObjectsWhenProducintHiddenView);
            Assert.True(file.Header.DisplayIntersectionPolylines);
            Assert.Equal(DxfXrefClippingBoundaryVisibility.DisplayedAndPlotted, file.Header.IsXRefClippingBoundaryVisible);

            // now test code 280 short
            file = Section("HEADER",
                (9, "$ACADVER"), (1, "AC1018"),
                (9, "$HIDETEXT"), (280, 1),
                (9, "$INTERSECTIONDISPLAY"), (280, 1),
                (9, "$XCLIPFRAME"), (280, 1)
            );
            Assert.True(file.Header.HideTextObjectsWhenProducintHiddenView);
            Assert.True(file.Header.DisplayIntersectionPolylines);
            Assert.Equal(DxfXrefClippingBoundaryVisibility.DisplayedAndPlotted, file.Header.IsXRefClippingBoundaryVisible);

            // verify that these variables aren't written twice
            file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2004;
            var text = ToString(file);

            Assert.True(text.IndexOf("$HIDETEXT") > 0); // make sure it's there
            Assert.Equal(text.IndexOf("$HIDETEXT"), text.LastIndexOf("$HIDETEXT")); // first and last should be the same

            Assert.True(text.IndexOf("$INTERSECTIONDISPLAY") > 0); // make sure it's there
            Assert.Equal(text.IndexOf("$INTERSECTIONDISPLAY"), text.LastIndexOf("$INTERSECTIONDISPLAY")); // first and last should be the same

            Assert.True(text.IndexOf("$XCLIPFRAME") > 0); // make sure it's there
            Assert.Equal(text.IndexOf("$XCLIPFRAME"), text.LastIndexOf("$XCLIPFRAME")); // first and last should be the same
        }

        [Fact]
        public void EndBlockHandleAndVersionCompatabilityTest()
        {
            var file = Section("BLOCKS",
                (0, "BLOCK"),
                (5, "20"),
                (330, "1F"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbBlockBegin"),
                (2, "*Model_Space"),
                (70, 0),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (3, "*Model_Space"),
                (1, ""),
                (0, "ENDBLK"),
                (5, "21"),
                (330, "1F"),
                (100, "AcDbEntity"),
                (8, "0"),
                (100, "AcDbBlockEnd")
            );
        }

        [Fact]
        public void ReadLineTypeElementsTest()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "LTYPE"),
                (0, "LTYPE"), // line type with 2 elements
                (2, "line-type-name"),
                (73, 2),
                (49, 1.0),
                (74, 0),
                (49, 2.0),
                (74, 1),
                (340, "DEADBEEF"), // pointer to the style object below
                (0, "ENDTAB"),
                (0, "TABLE"),
                (2, "STYLE"),
                (0, "STYLE"), // the style object
                (5, "DEADBEEF"),
                (2, "style-name"),
                (0, "ENDTAB")
            );
            var ltype = file.LineTypes.Where(l => l.Name == "line-type-name").Single();
            Assert.Equal(2, ltype.Elements.Count);

            Assert.Equal(1.0, ltype.Elements[0].DashDotSpaceLength);
            Assert.Equal(0, ltype.Elements[0].ComplexFlags);
            Assert.Null(ltype.Elements[0].Style);

            Assert.Equal(2.0, ltype.Elements[1].DashDotSpaceLength);
            Assert.Equal(1, ltype.Elements[1].ComplexFlags);
            Assert.Equal("style-name", ltype.Elements[1].Style.Name);
        }

        [Fact]
        public void WriteLineTypeElementsTest()
        {
            var file = new DxfFile();
            file.Clear();
            file.Header.Version = DxfAcadVersion.R2013;
            var ltype = new DxfLineType();
            file.LineTypes.Add(ltype);
            ltype.Name = "line-type-name";
            ltype.Elements.Add(new DxfLineTypeElement()
            {
                DashDotSpaceLength = 1.0,
                ComplexFlags = 0,
            });
            ltype.Elements.Add(new DxfLineTypeElement()
            {
                DashDotSpaceLength = 2.0,
                ComplexFlags = 0,
            });
            ltype.Elements.Add(new DxfLineTypeElement()
            {
                DashDotSpaceLength = 3.0,
                ComplexFlags = 0,
            });
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (49, 1.0),
                (74, 0),
                (49, 2.0),
                (74, 0),
                (49, 3.0),
                (74, 0)
            );
        }

        [Fact]
        public void WriteTablesWithDefaultValuesTest()
        {
            var file = new DxfFile();

            file.ApplicationIds.Add(new DxfAppId());
            file.ApplicationIds.Add(SetAllPropertiesToDefault(new DxfAppId()));

            file.BlockRecords.Add(new DxfBlockRecord());
            file.BlockRecords.Add(SetAllPropertiesToDefault(new DxfBlockRecord()));

            file.DimensionStyles.Add(new DxfDimStyle());
            file.DimensionStyles.Add(SetAllPropertiesToDefault(new DxfDimStyle()));

            file.Layers.Add(new DxfLayer());
            file.Layers.Add(SetAllPropertiesToDefault(new DxfLayer()));

            file.LineTypes.Add(new DxfLineType());
            file.LineTypes.Add(SetAllPropertiesToDefault(new DxfLineType()));

            file.Styles.Add(new DxfStyle());
            file.Styles.Add(SetAllPropertiesToDefault(new DxfStyle()));

            file.UserCoordinateSystems.Add(new DxfUcs());
            file.UserCoordinateSystems.Add(SetAllPropertiesToDefault(new DxfUcs()));

            file.Views.Add(new DxfView());
            file.Views.Add(SetAllPropertiesToDefault(new DxfView()));

            file.ViewPorts.Add(new DxfViewPort());
            file.ViewPorts.Add(SetAllPropertiesToDefault(new DxfViewPort()));

            using (var ms = new MemoryStream())
            {
                file.Save(ms);
            }
        }

        [Fact]
        public void WritingEmptyBlockR2000Test()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            file.Blocks.Add(new DxfBlock());
            file.Blocks.Add(SetAllPropertiesToDefault(new DxfBlock()));
            using (var ms = new MemoryStream())
            {
                file.Save(ms);
            }
        }

        [Fact]
        public void VerifyTableItemsReportTableAsOwner()
        {
            var file = new DxfFile();
            var view = new DxfView();
            file.Views.Add(view);
            using (var ms = new MemoryStream())
            {
                file.Save(ms); // not needed, but it forces pointers to bind
            }

            // check pointer values
            Assert.NotEqual(0u, view.OwnerHandle);
            Assert.Equal(view.OwnerHandle, file.TablesSection.ViewTable.Handle);

            // check object values
            Assert.True(ReferenceEquals(view.Owner, file.TablesSection.ViewTable));
        }

        [Fact]
        public void WriteLayerWithInvalidValuesTest()
        {
            var file = new DxfFile();
            var layer = file.Layers.Single();
            layer.Color = DxfColor.ByLayer; // code 62, value 256 not valid; normalized to 7
            layer.LineTypeName = null; // code 6, value null or empty not valid; normalized to CONTINUOUS
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "LAYER"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (2, "0"),
                (70, 0),
                (62, 7),
                (6, "CONTINUOUS")
            );
            layer.Color = DxfColor.ByBlock; // code 62, value 0 not valid; normalized to 7
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "LAYER"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (2, "0"),
                (70, 0),
                (62, 7),
                (6, "CONTINUOUS")
            );
        }

        [Fact]
        public void WriteViewPortWithInvalidValuesTest()
        {
            var file = new DxfFile();
            var viewPort = file.ViewPorts.First();
            viewPort.Name = "<viewPort>";

            // values must be positive; will be normalized on write
            viewPort.SnapSpacing = new DxfVector(double.NaN, double.PositiveInfinity, 0.0); // codes 14, 24; normalized to 1.0, 1.0
            viewPort.GridSpacing = default(DxfVector); // codes 15, 25; normalized to 1.0, 1.0
            viewPort.ViewHeight = -1.0; // code 40; normalized to 1.0
            viewPort.ViewPortAspectRatio = 0.0; // code 41; normalized to 1.0
            viewPort.LensLength = double.PositiveInfinity; // code 42; normalized to 50.0
            viewPort.ViewHeight = double.NegativeInfinity; // code 45; not written < R2007, normalized to 1.0
            viewPort.CircleSides = 0; // code 72; normalized to 1000
            viewPort.UCSIcon = -1; // code 74; normalized to 3
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "VPORT"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (2, "<viewPort>"),
                (70, 0),
                (10, 0.0),
                (20, 0.0),
                (11, 1.0),
                (21, 1.0),
                (12, 0.0),
                (22, 0.0),
                (13, 0.0),
                (23, 0.0),
                (14, 1.0),
                (24, 1.0),
                (15, 1.0),
                (25, 1.0),
                (16, 0.0),
                (26, 0.0),
                (36, 1.0),
                (17, 0.0),
                (27, 0.0),
                (37, 0.0),
                (40, 1.0),
                (41, 1.0),
                (42, 50.0),
                (43, 0.0),
                (44, 0.0),
                (50, 0.0),
                (51, 0.0),
                (71, 0),
                (72, 1000),
                (73, 1),
                (74, 3)
            );
            file.Header.Version = DxfAcadVersion.R2007;
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "VPORT"),
                (5, "#"),
                (330, "#"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbViewportTableRecord"),
                (2, "<viewPort>"),
                (70, 0),
                (10, 0.0),
                (20, 0.0),
                (11, 1.0),
                (21, 1.0),
                (12, 0.0),
                (22, 0.0),
                (13, 0.0),
                (23, 0.0),
                (14, 1.0),
                (24, 1.0),
                (15, 1.0),
                (25, 1.0),
                (16, 0.0),
                (26, 0.0),
                (36, 1.0),
                (17, 0.0),
                (27, 0.0),
                (37, 0.0),
                (42, 50.0),
                (43, 0.0),
                (44, 0.0),
                (45, 1.0)
            );
        }

        [Fact]
        public void WriteViewWithInvalidValuesTest()
        {
            var file = new DxfFile();
            var view = new DxfView();
            view.Name = "<view>";
            file.Views.Add(view);

            // values must be positive; will be normalized to 1.0 on write
            view.ViewHeight = -1.0; // code 40
            view.ViewWidth = 0.0; // code 41
            view.LensLength = double.NaN; // code 42
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "VIEW"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (2, "<view>"),
                (70, 0),
                (40, 1.0),
                (10, 0.0),
                (20, 0.0),
                (41, 1.0),
                (11, 0.0),
                (21, 0.0),
                (31, 1.0),
                (12, 0.0),
                (22, 0.0),
                (32, 0.0),
                (42, 1.0)
            );
        }

        [Fact]
        public void ReadZeroLayerColorTest()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "LAYER"),
                (0, "LAYER"),
                (2, "name"),
                (62, 0)
            );
            var layer = file.Layers.Single();
            Assert.True(layer.IsLayerOn);
            Assert.Equal((short)0, layer.Color.RawValue);
        }

        [Fact]
        public void ReadNormalLayerColorTest()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "LAYER"),
                (0, "LAYER"),
                (2, "name"),
                (62, 5)
            );
            var layer = file.Layers.Single();
            Assert.True(layer.IsLayerOn);
            Assert.Equal((short)5, layer.Color.RawValue);
        }

        [Fact]
        public void ReadNegativeLayerColorTest()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "LAYER"),
                (0, "LAYER"),
                (2, "name"),
                (62, -5)
            );
            var layer = file.Layers.Single();
            Assert.False(layer.IsLayerOn);
            Assert.Equal((short)5, layer.Color.RawValue);
        }

        [Fact]
        public void WriteNormalLayerColorTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            file.Layers.Add(new DxfLayer("name", DxfColor.FromIndex(5)) { IsLayerOn = true });
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "LAYER"),
                (5, "#"),
                (330, "#"),
                (100, "AcDbSymbolTableRecord"),
                (100, "AcDbLayerTableRecord"),
                (2, "name"),
                (70, 0),
                (62, 5)
            );
        }

        [Fact]
        public void WriteNegativeLayerColorTest()
        {
            var file = new DxfFile();
            file.Layers.Add(new DxfLayer("name", DxfColor.FromIndex(5)) { IsLayerOn = false });
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "LAYER"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (2, "name"),
                (70, 0),
                (62, -5)
            );
        }

        [Fact]
        public void SetActiveViewPortTest()
        {
            var file = new DxfFile();
            Assert.NotEqual(42.0, file.ActiveViewPort.ViewHeight);
            var newActive = new DxfViewPort()
            {
                Name = "*active",
                ViewHeight = 42.0,
            };
            file.ActiveViewPort = newActive;
            Assert.True(ReferenceEquals(newActive, file.ActiveViewPort));
        }

        [Fact]
        public void GetActiveViewPortTest()
        {
            var file = new DxfFile();
            file.ViewPorts.Clear();
            var one = new DxfViewPort() { Name = "one" };
            var two = new DxfViewPort() { Name = DxfViewPort.ActiveViewPortName };
            file.ViewPorts.Add(one);
            file.ViewPorts.Add(two);
            Assert.True(ReferenceEquals(two, file.ActiveViewPort));
        }

        [Fact]
        public void ReadUnicodeFromAsciiStreamTest()
        {
            // if version <= R2004 (AC1018) stream is ASCII

            // unicode values in the middle of the string
            var file = Section("HEADER", @"
  9
$ACADVER
  1
AC1018
  9
$PROJECTNAME
  1
Rep\U+00E8re pi\U+00E8ce
");
            Assert.Equal("Repère pièce", file.Header.ProjectName);

            // unicode values for the entire string
            file = Section("HEADER", @"
  9
$ACADVER
  1
AC1018
  9
$PROJECTNAME
  1
\U+4F60\U+597D
");
            Assert.Equal("你好", file.Header.ProjectName);
        }

        [Fact]
        public void WriteUnicodeToAsciiStreamTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2004;
            file.Header.ProjectName = "Repère pièce";
            var contents = ToString(file);
            Assert.Contains(@"Rep\U+00E8re pi\U+00E8ce", contents);
        }

        [Fact]
        public void ReadUnicodeFromUtf8StreamTest()
        {
            // if version >= R2007 (AC1021) stream is UTF8
            var file = Section("HEADER", @"
  9
$ACADVER
  1
AC1021
  9
$PROJECTNAME
  1
Repère pièce
");
            Assert.Equal("Repère pièce", file.Header.ProjectName);
        }

        [Fact]
        public void WriteUnicodeToUtf8StreamTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2007;
            file.Header.ProjectName = "Repère pièce";
            var contents = ToString(file);
            Assert.Contains("Repère pièce", contents);
        }

        [Fact]
        public void ReadFileWithExplicitNullEncodingTest()
        {
            // netstandard1.0 doesn't have `Encoding.ASCII` so `DxfFile` defaults it to null.  This test is to ensure
            // the null-handling behavior never changes.
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, Encoding.ASCII))
            {
                writer.WriteLine(@"
  0
EOF
".Trim());
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                var _file = DxfFile.Load(ms, defaultEncoding: null);
            }
        }

        [Fact]
        public void ReadGB18030EncodingTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var gb18030 = Encoding.GetEncoding("GB18030");

            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, Encoding.ASCII))
            {
                var head = @"
  0
SECTION
  2
HEADER
  9
$PROJECTNAME
  1".Trim();
                var tail = @"
  0
ENDSEC
  0
EOF".Trim();
                var gb18030bytes = new byte[]
                {
                    0xB2,
                    0xBB,
                };
                writer.WriteLine(head);
                writer.Flush();
                ms.Write(gb18030bytes, 0, gb18030bytes.Length);
                writer.WriteLine();
                writer.WriteLine(tail);
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                var file = DxfFile.Load(ms, gb18030);
                Assert.Equal("不", file.Header.ProjectName);
            }
        }

        [Fact]
        public void SelfReferencingRoundTripTest()
        {
            // ensure we don't enter an infinite loop with an item that owns itself

            // reading
            var file = Section("OBJECTS",
                (0, "DICTIONARY"),
                (5, "FFFF"),
                (330, "FFFF"),
                (3, "key"),
                (360, "FFFF")
            );
            var dict = (DxfDictionary)file.Objects.Single();
            Assert.Equal(dict, dict.Owner);
            Assert.Equal(dict, dict["key"]);

            // writing
            file.Header.Version = DxfAcadVersion.R14;
            var text = ToString(file);

            // reading again
            file = Parse(text);
            dict = (DxfDictionary)file.Objects.First();
            Assert.Equal(dict, dict.Owner);
            Assert.Equal(dict, dict["key"]);

            // ensure the pointer changed.  FFFF is unlikely to occur in writing
            Assert.NotEqual(0xFFFFu, ((IDxfItemInternal)dict).Handle);
        }

        [Fact]
        public void WriteSelfReferencingObjectRoundTripTest()
        {
            var dict = new DxfDictionary();
            dict["key"] = dict;

            var file = new DxfFile();
            file.Clear();
            file.Header.Version = DxfAcadVersion.R14;
            file.Objects.Add(dict);

            var text = ToString(file);
            file = Parse(text);
            dict = file.Objects.OfType<DxfDictionary>().Single(d => d.ContainsKey("key"));

            Assert.Equal(((IDxfItemInternal)dict).Handle, ((IDxfItemInternal)dict).OwnerHandle);
            Assert.Equal(((IDxfItemInternal)dict).Handle, ((IDxfItemInternal)dict["key"]).OwnerHandle);
        }

        [Fact]
        public void EnsureEntityHasNoDefaultOwner()
        {
            var file = Section("ENTITIES",
                (0, "POINT"),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0)
            );
            Assert.Null(file.Entities.Single().Owner);
        }

        [Fact]
        public void EnsureTableItemsHaveOwnersTest()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "LAYER"),
                (0, "LAYER"),
                (2, "layer-name"),
                (0, "ENDTAB")
            );
            var layer = file.Layers.Single();
            Assert.Equal("layer-name", layer.Name);
            Assert.Equal(file.TablesSection.LayerTable, layer.Owner);
        }

        [Fact]
        public void EnsureParsedFileHasNoDefaultItems()
        {
            var file = Parse((0, "EOF"));

            // all of these must be empty
            Assert.Equal(0, file.ApplicationIds.Count);
            Assert.Equal(0, file.BlockRecords.Count);
            Assert.Equal(0, file.Blocks.Count);
            Assert.Equal(0, file.Classes.Count);
            Assert.Equal(0, file.DimensionStyles.Count);
            Assert.Equal(0, file.Entities.Count);
            Assert.Equal(0, file.Layers.Count);
            Assert.Equal(0, file.LineTypes.Count);
            Assert.Null(file.RawThumbnail);

            // there is always a default dictionary
            Assert.Single(file.NamedObjectDictionary);
            Assert.Equal(1, file.Objects.Count);
            Assert.True(ReferenceEquals(file.NamedObjectDictionary, file.Objects.Single()));
            Assert.Equal("ACAD_GROUP", file.NamedObjectDictionary.Single().Key);
        }

        [Fact]
        public void DefaultTableItemsExistTest()
        {
            var file = new DxfFile();
            Assert.Equal(new[] { "ACAD", "ACADANNOTATIVE", "ACAD_NAV_VCDISPLAY", "ACAD_MLEADERVER" }, file.ApplicationIds.Select(a => a.Name).ToArray());
            Assert.Equal(new[] { "STANDARD", "ANNOTATIVE" }, file.DimensionStyles.Select(d => d.Name).ToArray());
            Assert.Equal("0", file.Layers.Single().Name);
            Assert.Equal(new[] { "BYLAYER", "BYBLOCK", "CONTINUOUS" }, file.LineTypes.Select(l => l.Name).ToArray());
            Assert.Equal("*ACTIVE", file.ViewPorts.Single().Name);
        }

        [Fact]
        public void DefaultTableItemsNotDuplicatedWithDifferingCaseTest()
        {
            // add lower case versions of expected items to an empty file
            var file = new DxfFile();
            file.Clear();
            var expectedAppIds = new[] { "acad", "acadannotative", "acad_nav_vcdisplay", "acad_mleaderver" };
            var expectedBlockRecords = new[] { "*model_space", "*paper_space" };
            var expectedDimStyles = new[] { "standard", "annotative" };
            var expectedLineTypes = new[] { "bylayer", "byblock", "continuous" };
            var expectedStyles = new[] { "standard", "annotative" };
            var expectedViewPorts = new[] { "*active" };
            var expectedLayers = new[] { "0" };

            foreach (var name in expectedAppIds)
            {
                file.ApplicationIds.Add(new DxfAppId() { Name = name });
            }

            foreach (var name in expectedBlockRecords)
            {
                file.BlockRecords.Add(new DxfBlockRecord() { Name = name });
            }

            foreach (var name in expectedDimStyles)
            {
                file.DimensionStyles.Add(new DxfDimStyle() { Name = name });
            }

            foreach (var name in expectedLineTypes)
            {
                file.LineTypes.Add(new DxfLineType() { Name = name });
            }

            foreach (var name in expectedStyles)
            {
                file.Styles.Add(new DxfStyle() { Name = name });
            }

            foreach (var name in expectedViewPorts)
            {
                file.ViewPorts.Add(new DxfViewPort() { Name = name });
            }

            foreach (var name in expectedLayers)
            {
                file.Layers.Add(new DxfLayer() { Name = name });
            }

            // normalize to ensure everything is there
            file.Normalize();

            // ensure there aren't duplicates of anything
            Assert.Equal(expectedAppIds.Length, file.ApplicationIds.Count);
            foreach (var expected in expectedAppIds)
            {
                Assert.Single(file.ApplicationIds.Where(x => string.Compare(x.Name, expected.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) == 0));
            }

            Assert.Equal(expectedBlockRecords.Length, file.BlockRecords.Count);
            foreach (var expected in expectedBlockRecords)
            {
                Assert.Single(file.BlockRecords.Where(x => string.Compare(x.Name, expected.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) == 0));
            }

            Assert.Equal(expectedDimStyles.Length, file.DimensionStyles.Count);
            foreach (var expected in expectedDimStyles)
            {
                Assert.Single(file.DimensionStyles.Where(x => string.Compare(x.Name, expected.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) == 0));
            }

            Assert.Equal(expectedLineTypes.Length, file.LineTypes.Count);
            foreach (var expected in expectedLineTypes)
            {
                Assert.Single(file.LineTypes.Where(x => string.Compare(x.Name, expected.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) == 0));
            }

            Assert.Equal(expectedStyles.Length, file.Styles.Count);
            foreach (var expected in expectedStyles)
            {
                Assert.Single(file.Styles.Where(x => string.Compare(x.Name, expected.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) == 0));
            }

            Assert.Equal(expectedViewPorts.Length, file.ViewPorts.Count);
            foreach (var expected in expectedViewPorts)
            {
                Assert.Single(file.ViewPorts.Where(x => string.Compare(x.Name, expected.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) == 0));
            }

            Assert.Equal(expectedLayers.Length, file.Layers.Count);
            foreach (var expected in expectedLayers)
            {
                Assert.Single(file.Layers.Where(x => string.Compare(x.Name, expected.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) == 0));
            }
        }

        [Fact]
        public void DefaultBlocksTest()
        {
            var file = new DxfFile();

            // validate defaults
            Assert.Equal(new[] { "*MODEL_SPACE", "*PAPER_SPACE" }, file.Blocks.Select(b => b.Name).ToArray());

            // ensure they're added back appropriately
            file.Blocks.Clear();
            file.Normalize();
            Assert.Equal(new[] { "*MODEL_SPACE", "*PAPER_SPACE" }, file.Blocks.Select(b => b.Name).ToArray());

            // ensure they're not duplicated in a different case
            file.Blocks.Clear();
            file.Blocks.Add(new DxfBlock() { Name = "*Model_Space" });
            file.Blocks.Add(new DxfBlock() { Name = "*Paper_Space" });
            file.Normalize();
            Assert.Equal(2, file.Blocks.Count);
            Assert.Equal("*Model_Space", file.Blocks[0].Name);
            Assert.Equal("*Paper_Space", file.Blocks[1].Name);
        }

        [Fact]
        public void AddMissingLayersOnNormalizeTest()
        {
            var file = new DxfFile();
            file.Entities.Add(new DxfLine() { Layer = null });
            file.Entities.Add(new DxfLine() { Layer = "some-layer" });
            file.Normalize();

            // ensure we can find the expected layer
            file.Layers.Single(l => l.Name == "some-layer");

            // ensure the `null` item wasn't added
            Assert.Empty(file.Layers.Where(l => l.Name == null));
        }

        [Fact]
        public void WriteAssociatedObjectsWithEntitiesTest()
        {
            // DxfImage has an associated DxfImageDefinition object
            var image = new DxfImage("imagePath", DxfPoint.Origin, 1, 1, new DxfVector(1.0, 1.0, 0.0));
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14; // DxfImage is only supported on >= R14
            file.Entities.Add(image);
            // image.ImageDefinition is explicitly not added to the Objects collection until the file is saved
            Assert.Empty(file.Objects.OfType<DxfImageDefinition>());
            VerifyFileContains(file,
                (0, "IMAGEDEF")
            );
            Assert.Equal("imagePath", file.Objects.OfType<DxfImageDefinition>().Single().FilePath);
        }

        [Fact]
        public void WriteAllDefaultEntitiesTest()
        {
            var file = new DxfFile();
            var assembly = typeof(DxfFile).GetTypeInfo().Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (IsEntityOrDerived(type))
                {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        // add the entity with its default initialized values
                        var entity = (DxfEntity)ctor.Invoke(new object[0]);
                        file.Entities.Add(entity);

                        // add the entity with its default values and 2 items added to each List<T> collection
                        entity = (DxfEntity)ctor.Invoke(new object[0]);
                        foreach (var property in type.GetProperties().Where(p => IsListOfT(p.PropertyType)))
                        {
                            var itemType = property.PropertyType.GenericTypeArguments.Single();
                            var itemValue = itemType.GetTypeInfo().IsValueType
                                ? Activator.CreateInstance(itemType)
                                : null;
                            var addMethod = property.PropertyType.GetMethod("Add");
                            var propertyValue = property.GetValue(entity);
                            addMethod.Invoke(propertyValue, new object[] { itemValue });
                            addMethod.Invoke(propertyValue, new object[] { itemValue });
                        }

                        // add an entity with all non-indexer properties set to `default(T)`
                        entity = (DxfEntity)SetAllPropertiesToDefault(ctor.Invoke(new object[0]));
                        file.Entities.Add(entity);
                    }
                }
            }

            // write each version of the entities with default versions
            foreach (var version in new[] { DxfAcadVersion.R10, DxfAcadVersion.R11, DxfAcadVersion.R12, DxfAcadVersion.R13, DxfAcadVersion.R14, DxfAcadVersion.R2000, DxfAcadVersion.R2004, DxfAcadVersion.R2007, DxfAcadVersion.R2010, DxfAcadVersion.R2013 })
            {
                file.Header.Version = version;
                using (var ms = new MemoryStream())
                {
                    file.Save(ms);
                }
            }
        }

        internal static bool IsEntityOrDerived(Type type)
        {
            if (type == typeof(DxfEntity))
            {
                return true;
            }

            if (type.GetTypeInfo().BaseType != null)
            {
                return IsEntityOrDerived(type.GetTypeInfo().BaseType);
            }

            return false;
        }

        internal static bool IsObjectOrDerived(Type type)
        {
            if (type == typeof(DxfObject))
            {
                return true;
            }

            if (type.GetTypeInfo().BaseType != null)
            {
                return IsObjectOrDerived(type.GetTypeInfo().BaseType);
            }

            return false;
        }
    }
}
