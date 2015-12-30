// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Sections;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfReaderWriterTests : AbstractDxfTests
    {
        [Fact]
        public void BinaryReaderTest()
        {
            // this file contains 12 lines
            var stream = new FileStream("diamond-bin.dxf", FileMode.Open, FileAccess.Read);
            var file = DxfFile.Load(stream);
            Assert.Equal(12, file.Entities.Count);
            Assert.Equal(12, file.Entities.Where(e => e.EntityType == DxfEntityType.Line).Count());
            var first = (DxfLine)file.Entities.First();
            Assert.Equal(new DxfPoint(45, 45, 0), first.P1);
            Assert.Equal(new DxfPoint(45, -45, 0), first.P2);
        }

        [Fact]
        public void ReadDxbTest()
        {
            var data = new byte[]
            {
                // DXB sentinel
                (byte)'A', (byte)'u', (byte)'t', (byte)'o', (byte)'C', (byte)'A', (byte)'D', (byte)' ', (byte)'D', (byte)'X', (byte)'B', (byte)' ', (byte)'1', (byte)'.', (byte)'0', (byte)'\r', (byte)'\n', 0x1A, 0x0,

                // color
                136, // type specifier for new color
                0x01, 0x00, // color index 1

                // line
                0x01, // type specifier
                0x01, 0x00, // P1.X = 0x0001
                0x02, 0x00, // P1.Y = 0x0002
                0x03, 0x00, // P1.Z = 0x0003
                0x04, 0x00, // P1.X = 0x0004
                0x05, 0x00, // P1.Y = 0x0005
                0x06, 0x00, // P1.Z = 0x0006

                // null terminator
                0x0
            };
            var stream = new MemoryStream(data);
            var file = DxfFile.Load(stream);
            var line = (DxfLine)file.Entities.Single();
            Assert.Equal(1, line.Color.RawValue);
            Assert.Equal(new DxfPoint(1, 2, 3), line.P1);
            Assert.Equal(new DxfPoint(4, 5, 6), line.P2);
        }

        [Fact]
        public void ReadDxbNoLengthOrPositionStreamTest()
        {
            var data = new byte[]
            {
                // DXB sentinel
                (byte)'A', (byte)'u', (byte)'t', (byte)'o', (byte)'C', (byte)'A', (byte)'D', (byte)' ', (byte)'D', (byte)'X', (byte)'B', (byte)' ', (byte)'1', (byte)'.', (byte)'0', (byte)'\r', (byte)'\n', 0x1A, 0x0,

                // color
                136, // type specifier for new color
                0x01, 0x00, // color index 1

                // line
                0x01, // type specifier
                0x01, 0x00, // P1.X = 0x0001
                0x02, 0x00, // P1.Y = 0x0002
                0x03, 0x00, // P1.Z = 0x0003
                0x04, 0x00, // P1.X = 0x0004
                0x05, 0x00, // P1.Y = 0x0005
                0x06, 0x00, // P1.Z = 0x0006

                // null terminator
                0x0
            };
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
                    var firstLine = DxfFile.GetFirstLine(binaryReader, out readBytes);
                    var dxfReader = DxfFile.GetCodePairReader(firstLine, readBytes, binaryReader);
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
                    var firstLine = DxfFile.GetFirstLine(binaryReader, out readBytes);
                    var dxfReader = DxfFile.GetCodePairReader(firstLine, readBytes, binaryReader);
                    var codePairs = dxfReader.GetCodePairs().ToList();

                    // verify code pair offsets correspond to line numbers
                    Assert.Equal(100, codePairs.Count);
                    Assert.Equal(22, codePairs.First().Offset);
                    Assert.Equal(805, codePairs.Last().Offset);
                }
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

        [Fact]
        public void SkipBomTest()
        {
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write((char)0xFEFF); // BOM
                writer.Write("0\r\nEOF");
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                var file = DxfFile.Load(stream);
                Assert.Equal(0, file.Layers.Count);
            }
        }

        [Fact]
        public void ReadThumbnailTest()
        {
            var file = Section("THUMBNAILIMAGE", @" 90
3
310
012345");
            AssertArrayEqual(file.RawThumbnail, new byte[] { 0x01, 0x23, 0x45 });
        }

        [Fact]
        public void WriteThumbnailTestR14()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            file.RawThumbnail = new byte[] { 0x01, 0x23, 0x45 };
            VerifyFileDoesNotContain(file, @"  0
SECTION
  2
THUMBNAILIMAGE");
        }

        [Fact]
        public void WriteThumbnailTestR2000()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            file.RawThumbnail = new byte[] { 0x01, 0x23, 0x45 };
            VerifyFileContains(file, @"
  0
SECTION
  2
THUMBNAILIMAGE
 90
3
310
012345
  0
ENDSEC");
        }

        [Fact]
        public void WriteThumbnailTest_SetThumbnailBitmap()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            var header = DxfThumbnailImageSection.BITMAPFILEHEADER;
            var bitmap = header.Concat(new byte[] { 0x01, 0x23, 0x45 }).ToArray();
            file.SetThumbnailBitmap(bitmap);
            VerifyFileContains(file, @"  0
SECTION
  2
THUMBNAILIMAGE
 90
3
310
012345
  0
ENDSEC");
        }

        [Fact]
        public void ReadThumbnailTest_GetThumbnailBitmap()
        {
            var file = Section("THUMBNAILIMAGE", @" 90
3
310
012345");
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
        public void ReadVersionSpecificClassTest_R13()
        {
            var file = Parse(@"
  0
SECTION
  2
HEADER
  9
$ACADVER
  1
AC1012
  0
ENDSEC
  0
SECTION
  2
CLASSES
  0
<class dxf name>
  1
CPP_CLASS_NAME
  2
<application name>
 90
42
  0
ENDSEC
  0
EOF
");
            Assert.Equal(1, file.Classes.Count);

            var cls = file.Classes.Single();
            Assert.Equal(cls.ClassDxfRecordName, "<class dxf name>");
            Assert.Equal(cls.CppClassName, "CPP_CLASS_NAME");
            Assert.Equal(cls.ApplicationName, "<application name>");
            Assert.Equal(cls.ClassVersionNumber, 42);
        }

        [Fact]
        public void ReadVersionSpecificClassTest_R14()
        {
            var file = Parse(@"
  0
SECTION
  2
HEADER
  9
$ACADVER
  1
AC1014
  0
ENDSEC
  0
SECTION
  2
CLASSES
  0
CLASS
  1
<class dxf name>
  2
CPP_CLASS_NAME
  3
<application name>
 90
42
  0
ENDSEC
  0
EOF
");
            Assert.Equal(1, file.Classes.Count);

            var cls = file.Classes.Single();
            Assert.Equal(cls.ClassDxfRecordName, "<class dxf name>");
            Assert.Equal(cls.CppClassName, "CPP_CLASS_NAME");
            Assert.Equal(cls.ApplicationName, "<application name>");
            Assert.Equal(cls.ProxyCapabilities.Value, 42);
        }

        [Fact]
        public void ReadVersionSpecificBlockRecordTest_R2000()
        {
            var file = Section("TABLES", @"
  0
TABLE
  2
BLOCK_RECORD
  5
2
330
0
100
AcDbSymbolTable
 70
0
  0
BLOCK_RECORD
  5
A
330
0
100
AcDbSymbolTableRecord
100
AcDbBlockTableRecord
  2
<name>
340
A1
310
010203040506070809
310
010203040506070809
1001
ACAD
1000
DesignCenter Data
1002
{
1070
0
1070
1
1070
2
1002
}
  0
ENDTAB
");
            var blockRecord = file.BlockRecords.Single();
            Assert.Equal("<name>", blockRecord.Name);
            Assert.Equal(0xA1u, blockRecord.LayoutHandle);

            var xdata = blockRecord.XData;
            Assert.Equal("ACAD", xdata.ApplicationName);
            Assert.Equal(2, xdata.Items.Count);

            Assert.Equal(DxfXDataType.String, xdata.Items[0].Type);
            Assert.Equal("DesignCenter Data", ((DxfXDataString)xdata.Items[0]).Value);

            var group = (DxfXDataControlGroup)xdata.Items[1];
            Assert.Equal(3, group.Items.Count);

            Assert.Equal(DxfXDataType.Integer, group.Items[0].Type);
            Assert.Equal((short)0, ((DxfXDataInteger)group.Items[0]).Value);

            Assert.Equal(DxfXDataType.Integer, group.Items[1].Type);
            Assert.Equal((short)1, ((DxfXDataInteger)group.Items[1]).Value);

            Assert.Equal(DxfXDataType.Integer, group.Items[2].Type);
            Assert.Equal((short)2, ((DxfXDataInteger)group.Items[2]).Value);

            AssertArrayEqual(new byte[]
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09
            }, blockRecord.BitmapData);
        }

        [Fact]
        public void WriteVersionSpecificBlockRecordTest_R2000()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            var blockRecord = new DxfBlockRecord()
            {
                Name = "<name>",
                OwnerHandle = 0x42u,
                LayoutHandle = 0x43u,
                XData = new DxfXData("ACAD",
                    new DxfXDataItem[]
                    {
                        new DxfXDataString("DesignCenter Data"),
                        new DxfXDataControlGroup(
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
            VerifyFileContains(file, @"
  0
TABLE
  2
BLOCK_RECORD
  5
9
100
AcDbSymbolTable
 70
1
  0
BLOCK_RECORD
  5
E
330
42
100
AcDbSymbolTableRecord
100
AcDbBlockTableRecord
  2
<name>
340
43
310
010203040506070809010203040506070809
1001
ACAD
1000
DesignCenter Data
1002
{
1070
0
1070
1
1070
2
1002
}
  0
ENDTAB
");
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
            VerifyFileContains(file, @"
  0
SECTION
  2
CLASSES
  0
<class dxf name>
  1
CPP_CLASS_NAME
  2
<application name>
 90
42
");
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
            VerifyFileContains(file, @"
  0
SECTION
  2
CLASSES
  0
CLASS
  1
<class dxf name>
  2
CPP_CLASS_NAME
  3
<application name>
 90
42
");
        }

        [Fact]
        public void ReadVersionSpecificBlockTest_R13()
        {
            var file = Parse(@"
  0
SECTION
  2
HEADER
  9
$ACADVER
  1
AC1012
  0
ENDSEC
  0
SECTION
  2
BLOCKS
  0
BLOCK
  5
42
100
AcDbEntity
  8
<layer>
100
AcDbBlockBegin
  2
<block name>
 70
0
 10
11
 20
22
 30
33
  3
<block name>
  1
<xref path>
  0
POINT
 10
1.1
 20
2.2
 30
3.3
  0
ENDBLK
  5
42
100
AcDbEntity
  8
<layer>
100
AcDbBlockEnd
  0
ENDSEC
  0
EOF
");

            var block = file.Blocks.Single();
            Assert.Equal("<block name>", block.Name);
            Assert.Equal(0x42u, block.Handle);
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
            var file = Parse(@"
  0
SECTION
  2
HEADER
  9
$ACADVER
  1
AC1014
  0
ENDSEC
  0
SECTION
  2
BLOCKS
  0
BLOCK
  5
42
100
AcDbEntity
  8
<layer>
100
AcDbBlockBegin
  2
<block name>
 70
0
 10
11
 20
22
 30
33
  3
<block name>
  1
<xref>
  0
POINT
 10
1.1
 20
2.2
 30
3.3
  0
ENDBLK
  5
42
100
AcDbBlockEnd
  0
ENDSEC
  0
EOF
");

            var block = file.Blocks.Single();
            Assert.Equal("<block name>", block.Name);
            Assert.Equal(0x42u, block.Handle);
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
            block.Handle = 0x42u;
            block.OwnerHandle = 0x43u;
            block.Layer = "<layer>";
            block.XrefName = "<xref>";
            block.BasePoint = new DxfPoint(11, 22, 33);
            block.Entities.Add(new DxfModelPoint(new DxfPoint(111, 222, 333)));
            file.Blocks.Add(block);
            VerifyFileContains(file, @"
  0
BLOCK
  5
42
330
43
100
AcDbEntity
  8
<layer>
100
AcDbBlockBegin
  2
<block name>
 70
0
 10
11.0
 20
22.0
 30
33.0
  3
<block name>
  1
<xref>
");
            VerifyFileContains(file, @"
 10
111.0
 20
222.0
 30
333.0
");
            VerifyFileContains(file, @"
  0
ENDBLK
  5
54
100
AcDbEntity
  8
<layer>
100
AcDbBlockEnd
  0
ENDSEC
");
        }

        [Fact]
        public void ReadTableTest()
        {
            var file = Parse(@"
  0
SECTION
  2
TABLES
  0
TABLE
  2
STYLE
  5
1C
102
{ACAD_XDICTIONARY
360
AAAA
360
BBBB
102
}
 70
3
1001
APP_X
1040
42.0
  0
STYLE
  5
3A
  2
ENTRY_1
 70
64
 40
0.4
 41
1.0
 50
0.0
 71
0
 42
0.4
  3
BUFONTS.TXT
  0
STYLE
  5
C2
  2
ENTRY_2
  3
BUFONTS.TXT
1001
APP_1
1070
45
1001
APP_2
1004
18A5B3EF2C199A
  0
ENDSEC
  0
EOF
");
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
            VerifyFileContains(file, @"
  0
TABLE
  2
STYLE
  5
4
100
AcDbSymbolTable
 70
3
");
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
            VerifyFileContains(file, @"
  0
TABLE
  2
STYLE
  5
4
102
{ACAD_XDICTIONARY
360
AAAA
360
BBBB
102
}
100
AcDbSymbolTable
");
        }

        [Fact]
        public void WriteVersionSpecificSectionsTest_R12()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R12;
            file.Classes.Add(new DxfClass());

            // no CLASSES section in R12
            VerifyFileDoesNotContain(file, @"
  0
SECTION
  2
CLASSES
");

            // no OBJECTS section in R12
            VerifyFileDoesNotContain(file, @"
  0
SECTION
  2
OBJECTS
");
        }

        [Fact]
        public void WriteVersionSpecificSectionsTest_R13()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R13;
            file.Classes.Add(new DxfClass());

            // CLASSES section added in R13
            VerifyFileContains(file, @"
  0
SECTION
  2
CLASSES
");

            // OBJECTS section added in R13
            // NYI
//            VerifyFileContains(file, @"
//  0
//SECTION
//  2
//OBJECTS
//");
        }

        [Fact]
        public void WriteVersionSpecificBlockRecordTest_R12()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R12;
            file.BlockRecords.Add(new DxfBlockRecord());

            // no BLOCK_RECORD in R12
            VerifyFileDoesNotContain(file, @"
  0
TABLE
  2
BLOCK_RECORD
");
        }

        [Fact]
        public void WriteVersionSpecificBlockRecordTest_R13()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R13;
            file.BlockRecords.Add(new DxfBlockRecord());

            // BLOCK_RECORD added in R13
            VerifyFileContains(file, @"
  0
TABLE
  2
BLOCK_RECORD
");
        }

        [Fact]
        public void Code280ShortInsteadOfCode290BoolTest()
        {
            // the spec says header variables $HIDETEXT, $INTERSECTIONDISPLAY,  and $XCLIPFRAME should be code 290
            // bools but some R2010 files encountered in the wild have a code 280 short instead

            // first test code 290 bool
            var file = Section("HEADER", @"
  9
$ACADVER
  1
AC1018
  9
$HIDETEXT
290
1
  9
$INTERSECTIONDISPLAY
290
1
  9
$XCLIPFRAME
290
1
");
            Assert.True(file.Header.HideTextObjectsWhenProducintHiddenView);
            Assert.True(file.Header.DisplayIntersectionPolylines);
            Assert.Equal(DxfXrefClippingBoundaryVisibility.DisplayedAndPlotted, file.Header.IsXRefClippingBoundaryVisible);

            // now test code 280 short
            file = Section("HEADER", @"
  9
$ACADVER
  1
AC1018
  9
$HIDETEXT
280
1
  9
$INTERSECTIONDISPLAY
280
1
  9
$XCLIPFRAME
280
1
");
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
        public void ReadStringWithControlCharactersTest()
        {
            var file = Section("ENTITIES", @"
  0
TEXT
  1
a^G^ ^^ b
");
            var text = (DxfText)file.Entities.Single();
            Assert.Equal("a\x7^\x1E b", text.Value);
        }

        [Fact]
        public void WriteStringWithControlCharactersTest()
        {
            var file = new DxfFile();
            file.Entities.Add(new DxfText() { Value = "a\x7^\x1E b" });
            VerifyFileContains(file, @"
  1
a^G^ ^^ b
");
        }

        [Fact]
        public void ParseWithInvariantCultureTest()
        {
            // from https://github.com/IxMilia/Dxf/issues/36
            var existingCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                var file = Section("HEADER", @"
  9
$TDCREATE
 40
2456478.590142998
");
                Assert.Equal(new DateTime(2013, 7, 4, 14, 9, 48, 355), file.Header.CreationDate);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = existingCulture;
            }
        }

        [Fact]
        public void WriteWithInvariantCultureTest()
        {
            var existingCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                var file = new DxfFile();
                file.Header.CreationDate = new DateTime(2013, 7, 4, 14, 9, 48, 355);
                VerifyFileContains(file, @"
$TDCREATE
 40
2456478.590143
");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = existingCulture;
            }
        }

        [Fact]
        public void EndBlockHandleAndVersionCompatabilityTest()
        {
            var file = Section("BLOCKS", @"
  0
BLOCK
  5
20
330
1F
100
AcDbEntity
  8
0
100
AcDbBlockBegin
  2
*Model_Space
 70
     0
 10
0.0
 20
0.0
 30
0.0
  3
*Model_Space
  1

  0
ENDBLK
  5
21
330
1F
100
AcDbEntity
  8
0
100
AcDbBlockEnd
");
        }

        [Fact]
        public void WriteTablesWithDefaultValuesTest()
        {
            var file = new DxfFile();

            file.ApplicationIds.Add(new DxfAppId());
            file.ApplicationIds.Add(SetAllPropertiesToDefault(new DxfAppId()));
            file.ApplicationIds.Add(null);

            file.BlockRecords.Add(new DxfBlockRecord());
            file.BlockRecords.Add(SetAllPropertiesToDefault(new DxfBlockRecord()));
            file.BlockRecords.Add(null);

            file.DimensionStyles.Add(new DxfDimStyle());
            file.DimensionStyles.Add(SetAllPropertiesToDefault(new DxfDimStyle()));
            file.DimensionStyles.Add(null);

            file.Layers.Add(new DxfLayer());
            file.Layers.Add(SetAllPropertiesToDefault(new DxfLayer()));
            file.Layers.Add(null);

            file.Linetypes.Add(new DxfLineType());
            file.Linetypes.Add(SetAllPropertiesToDefault(new DxfLineType()));
            file.Linetypes.Add(null);

            file.Styles.Add(new DxfStyle());
            file.Styles.Add(SetAllPropertiesToDefault(new DxfStyle()));
            file.Styles.Add(null);

            file.UserCoordinateSystems.Add(new DxfUcs());
            file.UserCoordinateSystems.Add(SetAllPropertiesToDefault(new DxfUcs()));
            file.UserCoordinateSystems.Add(null);

            file.Views.Add(new DxfView());
            file.Views.Add(SetAllPropertiesToDefault(new DxfView()));
            file.Views.Add(null);

            file.ViewPorts.Add(new DxfViewPort());
            file.ViewPorts.Add(SetAllPropertiesToDefault(new DxfViewPort()));
            file.ViewPorts.Add(null);

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
            file.Blocks.Add(null);
            using (var ms = new MemoryStream())
            {
                file.Save(ms);
            }
        }

        [Fact]
        public void ReadZeroLayerColorTest()
        {
            var file = Section("TABLES", @"
  0
TABLE
  2
LAYER
  0
LAYER
  2
name
 62
0
");
            var layer = file.Layers.Single();
            Assert.True(layer.IsLayerOn);
            Assert.Equal((short)0, layer.Color.RawValue);
        }

        [Fact]
        public void ReadNormalLayerColorTest()
        {
            var file = Section("TABLES", @"
  0
TABLE
  2
LAYER
  0
LAYER
  2
name
 62
5
");
            var layer = file.Layers.Single();
            Assert.True(layer.IsLayerOn);
            Assert.Equal((short)5, layer.Color.RawValue);
        }

        [Fact]
        public void ReadNegativeLayerColorTest()
        {
            var file = Section("TABLES", @"
  0
TABLE
  2
LAYER
  0
LAYER
  2
name
 62
-5
");
            var layer = file.Layers.Single();
            Assert.False(layer.IsLayerOn);
            Assert.Equal((short)5, layer.Color.RawValue);
        }

        [Fact]
        public void WriteNormalLayerColorTest()
        {
            var file = new DxfFile();
            file.Layers.Add(new DxfLayer("name", DxfColor.FromIndex(5)) { IsLayerOn = true });
            VerifyFileContains(file, @"
  0
LAYER
  5
14
100
AcDbSymbolTableRecord
  2
name
 70
0
 62
5
");
        }

        [Fact]
        public void WriteNegativeLayerColorTest()
        {
            var file = new DxfFile();
            file.Layers.Add(new DxfLayer("name", DxfColor.FromIndex(5)) { IsLayerOn = false });
            VerifyFileContains(file, @"
  0
LAYER
  5
14
100
AcDbSymbolTableRecord
  2
name
 70
0
 62
-5
");
        }

        [Fact]
        public void WriteAllDefaultEntitiesTest()
        {
            var file = new DxfFile();
            var assembly = typeof(DxfFile).Assembly;
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
                            var itemValue = itemType.IsValueType
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

        private static bool IsEntityOrDerived(Type type)
        {
            if (type == typeof(DxfEntity))
            {
                return true;
            }

            if (type.BaseType != null)
            {
                return IsEntityOrDerived(type.BaseType);
            }

            return false;
        }
    }
}
