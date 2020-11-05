using Xunit;

namespace IxMilia.Dxf.Test
{
    public class CodePairWriterTests : AbstractDxfTests
    {
        [Fact]
        public void WriteStringInBinary()
        {
            var actual = WriteToBinaryWriter(
                (1, "A")
            );
            var expected = new byte[]
            {
                0x01, 0x00, // code 1, string
                (byte)'A',
                0x00 // \0
            };
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WriteBinaryChunkInBinary()
        {
            var actual = WriteToBinaryWriter(
                (310, new byte[] { 0x01, 0x02 })
            );
            var expected = new byte[]
            {
                0x36, 0x01, // code 310, binary
                0x02, // length
                0x01, 0x02 // data
            };
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WriteBinaryChunkInText()
        {
            var actual = WriteToTextWriter(
                (310, new byte[] { 0x01, 0x02 })
            );
            var expected = "310\r\n0102\r\n";
            Assert.Equal(expected, actual);
        }
    }
}
