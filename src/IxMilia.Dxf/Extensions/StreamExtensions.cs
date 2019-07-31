// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;

namespace IxMilia.Dxf.Extensions
{
    internal static class StreamExtensions
    {
        public static string ReadLine(this Stream stream, out int bytesRead)
        {
            // read line char-by-char
            var sb = new StringBuilder();
            var b = stream.ReadByte();
            bytesRead = 1;
            if (b < 0)
            {
                return null;
            }

            var c = (char)b;
            while (b > 0 && c != '\n')
            {
                sb.Append(c);
                b = stream.ReadByte();
                bytesRead++;
                c = (char)b;
            }

            if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }
    }
}
