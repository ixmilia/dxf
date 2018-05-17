// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Sections
{
    internal class DxfThumbnailImageSection : DxfSection
    {
        public override DxfSectionType Type
        {
            get { return DxfSectionType.Thumbnail; }
        }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
            var list = new List<DxfCodePair>();
            list.Add(new DxfCodePair(90, RawData.Length));

            // write lines in 128-byte chunks (expands to 256 hex bytes)
            var hex = DxfCommonConverters.HexBytes(RawData);
            var lines = DxfCommonConverters.SplitIntoLines(hex, 256);
            list.AddRange(lines.Select(l => new DxfCodePair(310, l)));
            return list;
        }

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

        protected internal override void Clear()
        {
        }

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
                var lines = new List<string>();
                while (buffer.ItemsRemain)
                {
                    var pair = buffer.Peek();
                    buffer.Advance();

                    if (DxfCodePair.IsSectionEnd(pair))
                    {
                        break;
                    }

                    Debug.Assert(pair.Code == 310);
                    lines.Add(pair.StringValue);
                }

                var section = new DxfThumbnailImageSection();
                section.Clear();
#if NET35
                section.RawData = DxfCommonConverters.HexBytes(string.Join(string.Empty, lines.ToArray()));
#else
                section.RawData = DxfCommonConverters.HexBytes(string.Join(string.Empty, lines));
#endif
                return section;
            }

            return null;
        }
    }
}
