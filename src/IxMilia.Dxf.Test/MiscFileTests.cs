using System;
using System.IO;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class MiscFileTests : AbstractDxfTests
    {
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
    }
}
