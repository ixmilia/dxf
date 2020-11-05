using System.IO;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class TextWriterTests : AbstractDxfTests
    {
        protected static string WriteStringAsText(string value, DxfAcadVersion version)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new DxfWriter(ms, asText: true, version: version);
                writer.CreateInternalWriters();
                writer.WriteStringWithEncoding(value);
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        [Fact]
        public void WriteUnicodeToAsciiStreamTest()
        {
            var actual = WriteStringAsText("Repère pièce", DxfAcadVersion.R2004);
            var expected = "Rep\\U+00E8re pi\\U+00E8ce\r\n";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WriteUnicodeToUtf8StreamTest()
        {
            var actual = WriteStringAsText("Repère pièce", DxfAcadVersion.R2007);
            var expected = "Repère pièce\r\n";
            Assert.Equal(expected, actual);
        }
    }
}
