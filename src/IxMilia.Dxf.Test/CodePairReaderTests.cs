using System.IO;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class CodePairReaderTests : AbstractDxfTests
    {
        [Fact]
        public void ReadStringInBinary()
        {
            var buffer = new byte[]
            {
                0x01, 0x00, // code 1, string
                (byte)'A',
                0x00 // \0
            };
            using (var stream = new MemoryStream(buffer))
            using (var binReader = new BinaryReader(stream))
            {
                var reader = new DxfBinaryReader(binReader, isPostR13File: true);
                var pair = reader.GetCodePair();
                Assert.Equal(1, pair.Code);
                Assert.Equal("A", pair.StringValue);
            }
        }

        [Fact]
        public void ReadBinaryChunkInBinary()
        {
            var buffer = new byte[]
            {
                0x36, 0x01, // code 310, binary
                0x02, // length
                0x01, 0x02 // data
            };
            using (var stream = new MemoryStream(buffer))
            using (var binReader = new BinaryReader(stream))
            {
                var reader = new DxfBinaryReader(binReader, isPostR13File: true);
                var pair = reader.GetCodePair();
                Assert.Equal(310, pair.Code);
                Assert.Equal(new byte[] { 0x01, 0x02 }, pair.BinaryValue);
            }
        }

        [Fact]
        public void ReadBinaryChunkInText()
        {
            var reader = TextReaderFromLines(
                "310",
                "0102");
            var pair = reader.GetCodePair();
            Assert.Equal(310, pair.Code);
            Assert.Equal(new byte[] { 0x01, 0x02 }, pair.BinaryValue);
        }
    }
}
