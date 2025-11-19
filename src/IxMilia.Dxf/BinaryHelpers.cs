using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf
{
    internal static class BinaryHelpers
    {
        public static byte[] CombineBytes(IEnumerable<byte[]> data)
        {
            var result = new List<byte>();
            foreach (var d in data)
            {
                result.AddRange(d);
            }

            return result.ToArray();
        }

        public static IEnumerable<byte[]> ChunkBytes(byte[] data, int chunkSize)
        {
            if (data == null)
            {
                yield break;
            }

            var pos = 0;
            while (pos < data.Length)
            {
                var slice = data.Skip(pos).Take(chunkSize).ToArray();
                pos += slice.Length;
                yield return slice;
            }
        }

        public static IEnumerable<byte[]> ChunkBytes(byte[] data)
        {
            return ChunkBytes(data, 128);
        }
    }
}
