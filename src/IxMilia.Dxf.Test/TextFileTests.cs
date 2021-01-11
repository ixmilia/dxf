using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class TextFileTests : AbstractDxfTests
    {
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
        public void ReadFileWithExplicitNullEncodingTest()
        {
            // ensure that a `null` text encoding doesn't break file reading
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
        public void UseCodePageInNonUnicodeFilesTest()
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, Encoding.ASCII))
            {
                // R2004 means non-Unicode.  Characters are handled via $DWGCODEPAGE
                var head = @"
  0
SECTION
  2
HEADER
  9
$ACADVER
  1
AC1018
  9
$DWGCODEPAGE
  3
ANSI_1252
  9
$PROJECTNAME
  1".Trim();
                var tail = @"
  0
ENDSEC
  0
EOF".Trim();
                var ansi1252bytes = new byte[]
                {
                    0xDF, // German sharp S
                };
                writer.WriteLine(head);
                writer.Flush();
                ms.Write(ansi1252bytes, 0, ansi1252bytes.Length);
                writer.WriteLine();
                writer.WriteLine(tail);
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                var file = DxfFile.Load(ms, Encoding.ASCII); // force ASCII encoding that $DWGCODEPAGE will override
                Assert.Equal("ß", file.Header.ProjectName);
            }
        }

        // Using some example values from https://ezdxf.readthedocs.io/en/stable/dxfinternals/fileencoding.html
        [Theory]
        [InlineData("ANSI_874", 874)]
        [InlineData("ANSI_1252", 1252)]
        [InlineData("ansi_1252", 1252)]
        public void ParseCodePageTest(string dxfLine, int expectedCodePage)
        {
            Assert.True(DxfEncodingHelper.TryParseEncoding(dxfLine, out var actualCodePage));
            Assert.Equal(expectedCodePage, actualCodePage);
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void ReadWithDifferingNewLinesTest(string newline)
        {
            var lines = new List<string>()
            {
                "0", "SECTION",
                "2", "ENTITIES",
                "0", "LINE",
                "0", "ENDSEC",
                "0", "EOF"
            };

            using (var ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
                {
                    writer.Write(string.Join(newline, lines));
                }

                ms.Seek(0, SeekOrigin.Begin);
                var file = DxfFile.Load(ms);
                Assert.IsType<DxfLine>(file.Entities.Single());
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
    }
}
