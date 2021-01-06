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
            yield return new DxfCodePair(90, RawData.Length);

            // write lines in 128-byte chunks (expands to 256 hex bytes)
            foreach (var chunk in BinaryHelpers.ChunkBytes(RawData))
            {
                yield return new DxfCodePair(310, chunk);
            }
        }

        public byte[] RawData { get; set; }

        public byte[] GetThumbnailBitmap()
        {
            var result = new byte[RawData.Length + BITMAPFILEHEADER.Length];

            // populate the bitmap header
            Array.Copy(BITMAPFILEHEADER, 0, result, 0, BITMAPFILEHEADER.Length);

            // write the file length
            var lengthBytes = BitConverter.GetBytes(result.Length);
            Array.Copy(lengthBytes, 0, result, BITMAPFILELENGTHOFFSET, lengthBytes.Length);

            // write the image data offset if it can be safely calculated
            if (RawData.Length >= 36)
            {
                var dibHeaderSize = BitConverter.ToInt32(RawData, 0);
                var paletteColorCount = BitConverter.ToInt32(RawData, 32); // in all valid headers, the palette count is at this location
                var paletteSize = paletteColorCount * 4; // always 4 values; BGRA
                var imageDataOffset = BITMAPFILEHEADER.Length + dibHeaderSize + paletteSize;
                var imageDataOffsetBytes = BitConverter.GetBytes(imageDataOffset);
                Array.Copy(imageDataOffsetBytes, 0, result, IMAGEDATAOFFSETOFFSET, imageDataOffsetBytes.Length);
            }

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
                    0x36, 0x04, 0x00, 0x00 // bit offset; assumed to be 1078 which means BITMAPINFOHEADER + 256 element palette
                };
            }
        }

        private const int BITMAPFILELENGTHOFFSET = 2;
        private const int IMAGEDATAOFFSETOFFSET = 10;

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
                var rawData = new List<byte>();
                while (buffer.ItemsRemain)
                {
                    var pair = buffer.Peek();
                    buffer.Advance();

                    if (DxfCodePair.IsSectionEnd(pair))
                    {
                        break;
                    }

                    Debug.Assert(pair.Code == 310);
                    rawData.AddRange(pair.BinaryValue);
                }

                var section = new DxfThumbnailImageSection();
                section.Clear();
                section.RawData = rawData.ToArray();
                return section;
            }

            return null;
        }
    }
}
