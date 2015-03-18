// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace IxMilia.Dxf.Sections
{
    internal class DxfThumbnailImageSection : DxfSection
    {
        public override DxfSectionType Type
        {
            get { return DxfSectionType.Thumbnail; }
        }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version)
        {
            var list = new List<DxfCodePair>();
            list.Add(new DxfCodePair(90, RawData.Length));

            // write lines in 128-byte chunks (expands to 256 hex bytes)
            var sb = new StringBuilder();
            var chunkCount = (int)Math.Ceiling((double)RawData.Length / ChunkSize);
            for (int i = 0; i < chunkCount; i++)
            {
                sb.Clear();
                for (int offset = i * ChunkSize; offset < ChunkSize && offset < RawData.Length; offset++)
                {
                    sb.Append(RawData[offset].ToString("X2"));
                }

                list.Add(new DxfCodePair(310, sb.ToString()));
            }

            return list;
        }

        private const int ChunkSize = 128;

        public byte[] RawData { get; set; }

        public byte[] GetThumbnailBitmap()
        {
            var result = new byte[RawData.Length + BITMAPFILEHEADER.Length];

            // populate the bitmap header
            Array.Copy(BITMAPFILEHEADER, 0, result, 0, BITMAPFILEHEADER.Length);

            // write the file length
            var lengthBytes = BitConverter.GetBytes(RawData.Length);
            Array.Copy(lengthBytes, 0, result, BITMAPFILELENGTHOFFSET, lengthBytes.Length);

            // copy the raw data
            Array.Copy(RawData, 0, result, BITMAPFILEHEADER.Length, RawData.Length);

            return result;
        }

        public void SetThumbnailBitmap(byte[] thumbnail)
        {
            // strip off bitmap header
            Debug.Assert(thumbnail != null);
            Debug.Assert(thumbnail.Length > BITMAPFILEHEADER.Length);
            Debug.Assert(thumbnail[0] == 'B');
            Debug.Assert(thumbnail[1] == 'M');
            RawData = new byte[thumbnail.Length - BITMAPFILEHEADER.Length];
            Array.Copy(thumbnail, BITMAPFILEHEADER.Length, RawData, 0, RawData.Length);
        }

        // BITMAPFILEHEADER structure
        internal static byte[] BITMAPFILEHEADER
        {
            get
            {
                return new byte[]
                {
                    (byte)'B', (byte)'M', // magic number
                    0x00, 0x00, 0x00, 0x00, // file length (calculated later)
                    0x00, 0x00, // reserved
                    0x00, 0x00, // reserved
                    0x36, 0x04, 0x00, 0x00 // bit offset; always 1078
                };
            }
        }

        private const int BITMAPFILELENGTHOFFSET = 2;

        internal static DxfThumbnailImageSection ThumbnailImageSectionFromBuffer(DxfCodePairBufferReader buffer)
        {
            if (buffer.ItemsRemain)
            {
                var lengthPair = buffer.Peek();
                buffer.Advance();

                if (lengthPair.Code != 90)
                {
                    return null;
                }

                var length = lengthPair.IntegerValue;
                var data = new byte[length];
                var position = 0;
                while (buffer.ItemsRemain)
                {
                    var pair = buffer.Peek();
                    buffer.Advance();

                    if (DxfCodePair.IsSectionEnd(pair))
                    {
                        break;
                    }

                    Debug.Assert(pair.Code == 310);
                    var written = CopyHexToBuffer(pair.StringValue, data, position);
                    position += written;
                }

                var section = new DxfThumbnailImageSection();
                section.RawData = data;
                return section;
            }

            return null;
        }

        private static int CopyHexToBuffer(string data, byte[] buffer, int offset)
        {
            for (int i = 0; i < data.Length; i += 2)
            {
                buffer[offset] = HexToByte(data[i], data[i + 1]);
                offset++;
            }

            return data.Length / 2;
        }

        private static byte HexToByte(char c1, char c2)
        {
            return (byte)((HexToByte(c1) << 4) + HexToByte(c2));
        }

        private static byte HexToByte(char c)
        {
            switch (c)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'a':
                case 'A': return 10;
                case 'b':
                case 'B': return 11;
                case 'c':
                case 'C': return 12;
                case 'd':
                case 'D': return 13;
                case 'e':
                case 'E': return 14;
                case 'f':
                case 'F': return 15;
                default:
                    return 0;
            }
        }
    }
}
