// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IxMilia.Dxf.Extensions
{
    internal static class StreamExtensions
    {
        public static string ReadLine(this Stream stream, Encoding encoding, out int bytesRead)
        {
            // read line char-by-char
            bytesRead = 0;
            var bytes = new List<byte>();
            var b = stream.ReadByte();
            if (b < 0)
            {
                return null;
            }

            bytesRead++;

            while (b > 0 && b != '\n')
            {
                bytes.Add((byte)b);
                b = stream.ReadByte();
                bytesRead++;
            }

            var byteArray = bytes.ToArray();
            var result = GetString(encoding, byteArray);
            if (result.Length > 0 && result[result.Length - 1] == '\r')
            {
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }

        private static string GetString(Encoding encoding, byte[] bytes)
        {
            if (encoding == null)
            {
                var sb = new StringBuilder(bytes.Length);
                foreach (var b in bytes)
                {
                    sb.Append((char)b);
                }

                return sb.ToString();
            }
            else
            {
                return encoding.GetString(bytes, 0, bytes.Length);
            }
        }
    }
}
