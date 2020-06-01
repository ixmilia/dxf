using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf
{
    internal static class BinaryHelpers
    {
        public static byte[] ByteArrayFromStrings(IEnumerable<string> lines)
        {
            return DxfCommonConverters.HexBytes(string.Join(string.Empty, lines.ToArray()));
        }

        public static IEnumerable<string> StringsFromByteArray(byte[] bytes)
        {
            // write lines in 128-byte chunks (expands to 256 hex bytes)
            var hex = DxfCommonConverters.HexBytes(bytes);
            var lines = DxfCommonConverters.SplitIntoLines(hex, 256);
            return lines;
        }
    }
}
