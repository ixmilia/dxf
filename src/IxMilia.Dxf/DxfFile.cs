// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Objects;
using IxMilia.Dxf.Sections;

namespace IxMilia.Dxf
{
    public class DxfFile
    {
        public const string BinarySentinel = "AutoCAD Binary DXF";
        public const string EofText = "EOF";

        internal DxfHeaderSection HeaderSection { get; private set; }
        internal DxfClassesSection ClassSection { get; private set; }
        internal DxfTablesSection TablesSection { get; private set; }
        internal DxfBlocksSection BlocksSection { get; private set; }
        internal DxfEntitiesSection EntitiesSection { get; private set; }
        internal DxfObjectsSection ObjectsSection { get; private set; }
        internal DxfThumbnailImageSection ThumbnailImageSection { get; private set; }

        public List<DxfEntity> Entities { get { return EntitiesSection.Entities; } }

        public List<DxfClass> Classes { get { return ClassSection.Classes; } }

        public List<DxfBlock> Blocks { get { return BlocksSection.Blocks; } }

        public List<DxfObject> Objects { get { return ObjectsSection.Objects; } }

        public DxfHeader Header { get { return HeaderSection.Header; } }

        public List<DxfLayer> Layers { get { return TablesSection.LayerTable.Items; } }

        public List<DxfViewPort> ViewPorts { get { return TablesSection.ViewPortTable.Items; } }

        public List<DxfDimStyle> DimensionStyles { get { return TablesSection.DimStyleTable.Items; } }

        public List<DxfView> Views { get { return TablesSection.ViewTable.Items; } }

        public List<DxfUcs> UserCoordinateSystems { get { return TablesSection.UcsTable.Items; } }

        public List<DxfAppId> ApplicationIds { get { return TablesSection.AppIdTable.Items; } }

        public List<DxfBlockRecord> BlockRecords { get { return TablesSection.BlockRecordTable.Items; } }

        public List<DxfLineType> Linetypes { get { return TablesSection.LTypeTable.Items; } }

        public List<DxfStyle> Styles { get { return TablesSection.StyleTable.Items; } }

        /// <summary>
        /// Gets the thumbnail bitmap.
        /// </summary>
        /// <returns>Raw bytes that should serialize to a .BMP file.</returns>
        public byte[] GetThumbnailBitmap()
        {
            return ThumbnailImageSection == null ? null : ThumbnailImageSection.GetThumbnailBitmap();
        }

        /// <summary>
        /// Sets the bitmap thumbnail.
        /// </summary>
        /// <param name="thumbnail">Raw data of the thumbnail image.  Should be a 256-color bitmap, 180 pixels wide, any height.</param>
        public void SetThumbnailBitmap(byte[] thumbnail)
        {
            if (ThumbnailImageSection == null)
                ThumbnailImageSection = new DxfThumbnailImageSection();
            ThumbnailImageSection.SetThumbnailBitmap(thumbnail);
        }

        /// <summary>
        /// Raw data of the thumbnail image.  Should be a 256-color bitmap, 180 pixels wide, any height.
        /// </summary>
        public byte[] RawThumbnail
        {
            get { return ThumbnailImageSection == null ? null : ThumbnailImageSection.RawData; }
            set
            {
                if (value == null)
                {
                    ThumbnailImageSection = null;
                }
                else
                {
                    if (ThumbnailImageSection == null)
                        ThumbnailImageSection = new DxfThumbnailImageSection();
                    ThumbnailImageSection.RawData = value;
                }
            }
        }

        internal IEnumerable<DxfSection> Sections
        {
            get
            {
                yield return this.HeaderSection;
                if (Header.Version >= DxfAcadVersion.R13)
                {
                    yield return this.ClassSection;
                }

                yield return this.TablesSection;
                yield return this.BlocksSection;
                yield return this.EntitiesSection;
                if (Header.Version >= DxfAcadVersion.R13)
                {
                    yield return this.ObjectsSection;
                }

                if (Header.Version >= DxfAcadVersion.R2000 && this.ThumbnailImageSection != null)
                {
                    yield return this.ThumbnailImageSection;
                }
            }
        }

        public DxfFile()
        {
            this.HeaderSection = new DxfHeaderSection();
            this.ClassSection = new DxfClassesSection();
            this.TablesSection = new DxfTablesSection();
            this.BlocksSection = new DxfBlocksSection();
            this.EntitiesSection = new DxfEntitiesSection();
            this.ObjectsSection = new DxfObjectsSection();
            this.ThumbnailImageSection = null; // not always present
        }

        public static DxfFile Load(Stream stream)
        {
            var reader = new BinaryReader(stream);
            int readBytes;
            var firstLine = GetFirstLine(reader, out readBytes);

            // check for binary sentinels
            DxfFile file;
            if (firstLine == DxbReader.BinarySentinel)
            {
                file = new DxbReader().ReadFile(reader);
            }
            else
            {
                var dxfReader = GetCodePairReader(firstLine, readBytes, reader);
                file = LoadFromReader(dxfReader);
            }

            return file;
        }

        internal static string GetFirstLine(BinaryReader binaryReader, out int readBytes)
        {
            // read first line char-by-char
            readBytes = 0;
            var sb = new StringBuilder();
            var c = binaryReader.ReadChar();
            readBytes++;
            while (c != '\n')
            {
                sb.Append(c);
                c = binaryReader.ReadChar();
                readBytes++;
            }

            // trim BOM
            var line = sb.ToString().TrimEnd('\r');
            if (line.Length > 0 && line[0] == 0xFEFF)
            {
                line = line.Substring(1);
            }

            return line;
        }

        internal static IDxfCodePairReader GetCodePairReader(string firstLine, int readBytes, BinaryReader binaryReader)
        {
            if (firstLine == DxbReader.BinarySentinel)
            {
                throw new DxfReadException("DXB files don't support code pairs.  This path should never be hit.", readBytes);
            }
            else
            {
                IDxfCodePairReader dxfReader;
                if (firstLine == BinarySentinel)
                {
                    dxfReader = new DxfBinaryReader(binaryReader, readBytes);
                }
                else
                {
                    dxfReader = new DxfAsciiReader(binaryReader.BaseStream, firstLine);
                }

                return dxfReader;
            }
        }

        private static DxfFile LoadFromReader(IDxfCodePairReader reader)
        {
            var file = new DxfFile();
            file.Clear();
            var buffer = new DxfCodePairBufferReader(reader.GetCodePairs());
            var version = DxfAcadVersion.R14;
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (DxfCodePair.IsSectionStart(pair))
                {
                    buffer.Advance(); // swallow (0, SECTION) pair
                    var section = DxfSection.FromBuffer(buffer, version);
                    if (section != null)
                    {
                        switch (section.Type)
                        {
                            case DxfSectionType.Blocks:
                                file.BlocksSection = (DxfBlocksSection)section;
                                break;
                            case DxfSectionType.Entities:
                                file.EntitiesSection = (DxfEntitiesSection)section;
                                break;
                            case DxfSectionType.Classes:
                                file.ClassSection = (DxfClassesSection)section;
                                break;
                            case DxfSectionType.Header:
                                file.HeaderSection = (DxfHeaderSection)section;
                                version = file.Header.Version;
                                break;
                            case DxfSectionType.Objects:
                                file.ObjectsSection = (DxfObjectsSection)section;
                                break;
                            case DxfSectionType.Tables:
                                file.TablesSection = (DxfTablesSection)section;
                                break;
                            case DxfSectionType.Thumbnail:
                                file.ThumbnailImageSection = (DxfThumbnailImageSection)section;
                                break;
                        }
                    }
                }
                else if (DxfCodePair.IsEof(pair))
                {
                    // swallow and quit
                    buffer.Advance();
                    break;
                }
                else if (DxfCodePair.IsComment(pair))
                {
                    // swallow comments
                    buffer.Advance();
                }
                else
                {
                    // swallow unexpected code pair
                    buffer.Advance();
                }
            }

            Debug.Assert(!buffer.ItemsRemain);
            file.Header.NextAvailableHandle = file.SetHandles();

            DxfPointer.BindPointers(file);

            return file;
        }

        public void Save(Stream stream, bool asText = true)
        {
            WriteStream(stream, asText);
        }

        public void SaveDxb(Stream stream)
        {
            new DxbWriter(stream).Save(this);
        }

        private void WriteStream(Stream stream, bool asText)
        {
            var writer = new DxfWriter(stream, asText);
            writer.Open();

            var nextHandle = SetHandles();
            Header.NextAvailableHandle = nextHandle;

            DxfPointer.AssignPointers(this);

            // write sections
            var outputHandles = Header.Version >= DxfAcadVersion.R13 || Header.HandlesEnabled; // handles are always enabled on R13+
            foreach (var section in Sections)
            {
                foreach (var pair in section.GetValuePairs(Header.Version, outputHandles))
                    writer.WriteCodeValuePair(pair);
            }

            writer.Close();
        }

        internal IEnumerable<IDxfItemInternal> GetFileItems()
        {
            return Objects;
        }

        private IEnumerable<IDxfHasHandle> HandleItems
        {
            get
            {
                return this.TablesSection.GetTables(Header.Version).Cast<IDxfHasHandle>()
                    .Concat(this.ApplicationIds.Cast<IDxfHasHandle>())
                    .Concat(this.BlockRecords.Cast<IDxfHasHandle>())
                    .Concat(this.Blocks.Cast<IDxfHasHandle>())
                    .Concat(this.DimensionStyles.Cast<IDxfHasHandle>())
                    .Concat(this.Entities.Cast<IDxfHasHandle>())
                    .Concat(this.Layers.Cast<IDxfHasHandle>())
                    .Concat(this.Linetypes.Cast<IDxfHasHandle>())
                    //.Concat(this.Objects.Cast<IDxfHasHandle>())
                    .Concat(this.Styles.Cast<IDxfHasHandle>())
                    .Concat(this.UserCoordinateSystems.Cast<IDxfHasHandle>())
                    .Concat(this.ViewPorts.Cast<IDxfHasHandle>())
                    .Concat(this.Views.Cast<IDxfHasHandle>())
                    .Where(item => item != null);
            }
        }

        private uint SetHandles()
        {
            uint largestHandle = 0u;

            foreach (var item in HandleItems)
            {
                largestHandle = Math.Max(largestHandle, item.Handle);
                var parent = item as IDxfHasChildrenWithHandle;
                if (parent != null)
                {
                    foreach (var child in parent.GetChildren())
                    {
                        largestHandle = Math.Max(largestHandle, child.Handle);
                    }
                }
            }

            var nextHandle = largestHandle + 1;

            foreach (var item in HandleItems)
            {
                if (item.Handle == 0u)
                {
                    item.Handle = nextHandle++;
                }

                var parent = item as IDxfHasChildrenWithHandle;
                if (parent != null)
                {
                    foreach (var child in parent.GetChildren())
                    {
                        if (child as IDxfHasOwnerHandle != null)
                        {
                            ((IDxfHasOwnerHandle)child).OwnerHandle = item.Handle;
                        }

                        if (child.Handle == 0u)
                        {
                            child.Handle = nextHandle++;
                        }
                    }
                }
            }

            return nextHandle;
        }

        public void Clear()
        {
            foreach (var section in Sections)
            {
                section.Clear();
            }
        }
    }
}
