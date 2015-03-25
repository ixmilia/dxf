// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
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
        }

        [Fact]
        public void SkipBomTest()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write((char)0xFEFF); // BOM
            writer.Write("0\r\nEOF");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            var file = DxfFile.Load(stream);
            Assert.Equal(0, file.Layers.Count);
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
<handle>
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
<handle>
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
            Assert.Equal("<handle>", block.Handle);
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
<handle>
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
<handle>
100
AcDbBlockEnd
  0
ENDSEC
  0
EOF
");

            var block = file.Blocks.Single();
            Assert.Equal("<block name>", block.Name);
            Assert.Equal("<handle>", block.Handle);
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
            block.Handle = "<handle>";
            block.Layer = "<layer>";
            block.XrefName = "<xref>";
            block.BasePoint = new DxfPoint(11, 22, 33);
            block.Entities.Add(new DxfModelPoint(new DxfPoint(111, 222, 333)));
            file.Blocks.Add(block);
            VerifyFileContains(file, @"
  0
SECTION
  2
BLOCKS
  0
BLOCK
  5
<handle>
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
1.1000000000000000E+001
 20
2.2000000000000000E+001
 30
3.3000000000000000E+001
  3
<block name>
  1
<xref>
");
            VerifyFileContains(file, @"
 10
1.1100000000000000E+002
 20
2.2200000000000000E+002
 30
3.3300000000000000E+002
");
            VerifyFileContains(file, @"
  0
ENDBLK
  5
<handle>
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
        public void WriteVersionSpecificBlockTest_R14()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            var block = new DxfBlock();
            block.Name = "<block name>";
            block.Handle = "<handle>";
            block.Layer = "<layer>";
            block.XrefName = "<xref>";
            block.BasePoint = new DxfPoint(11, 22, 33);
            block.Entities.Add(new DxfModelPoint(new DxfPoint(111, 222, 333)));
            file.Blocks.Add(block);
            VerifyFileContains(file, @"
  0
SECTION
  2
BLOCKS
  0
BLOCK
  5
<handle>
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
1.1000000000000000E+001
 20
2.2000000000000000E+001
 30
3.3000000000000000E+001
  3
<block name>
  1
<xref>
");
            VerifyFileContains(file, @"
 10
1.1100000000000000E+002
 20
2.2200000000000000E+002
 30
3.3300000000000000E+002
");
            VerifyFileContains(file, @"
  0
ENDBLK
  5
<handle>
100
AcDbBlockEnd
  0
ENDSEC
");
        }
    }
}
