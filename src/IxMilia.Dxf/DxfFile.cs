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

        private DateTime _lastOpenOrSave;

        public IList<DxfEntity> Entities { get { return EntitiesSection.Entities; } }

        public IList<DxfClass> Classes { get { return ClassSection.Classes; } }

        public IList<DxfBlock> Blocks { get { return BlocksSection.Blocks; } }

        public IList<DxfObject> Objects { get { return ObjectsSection.Objects; } }

        public DxfHeader Header { get { return HeaderSection.Header; } }

        public IList<DxfLayer> Layers { get { return TablesSection.LayerTable.Items; } }

        public IList<DxfViewPort> ViewPorts { get { return TablesSection.ViewPortTable.Items; } }

        public IList<DxfDimStyle> DimensionStyles { get { return TablesSection.DimStyleTable.Items; } }

        public IList<DxfView> Views { get { return TablesSection.ViewTable.Items; } }

        public IList<DxfUcs> UserCoordinateSystems { get { return TablesSection.UcsTable.Items; } }

        public IList<DxfAppId> ApplicationIds { get { return TablesSection.AppIdTable.Items; } }

        public IList<DxfBlockRecord> BlockRecords { get { return TablesSection.BlockRecordTable.Items; } }

        public IList<DxfLineType> LineTypes { get { return TablesSection.LTypeTable.Items; } }

        public IList<DxfStyle> Styles { get { return TablesSection.StyleTable.Items; } }

        public DxfDictionary NamedObjectDictionary { get { return Objects.FirstOrDefault() as DxfDictionary; } }

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
            _lastOpenOrSave = DateTime.UtcNow;
            this.HeaderSection = new DxfHeaderSection();
            this.ClassSection = new DxfClassesSection();
            this.TablesSection = new DxfTablesSection();
            this.BlocksSection = new DxfBlocksSection();
            this.EntitiesSection = new DxfEntitiesSection();
            this.ObjectsSection = new DxfObjectsSection();
            this.ThumbnailImageSection = null; // not always present
            this.Normalize();
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

        private void UpdateTimes()
        {
            var currentTime = DateTime.Now;
            var currentTimeUtc = DateTime.UtcNow;
            var timeInDrawing = currentTimeUtc - _lastOpenOrSave;

            Header.TimeInDrawing += timeInDrawing;
            Header.UpdateDate = currentTime;
            Header.UpdateDateUniversal = currentTimeUtc;
            if (Header.UserTimerOn)
            {
                Header.UserElapsedTimer += timeInDrawing;
            }

            _lastOpenOrSave = currentTimeUtc;
        }

        private void WriteStream(Stream stream, bool asText)
        {
            var writer = PrepareWriter(stream, asText);
            WriteSectionsAndClose(writer, Sections);
        }

        private DxfWriter PrepareWriter(Stream stream, bool asText)
        {
            UpdateTimes();
            Normalize();

            var writer = new DxfWriter(stream, asText);
            writer.Open();

            var nextHandle = DxfPointer.AssignHandles(this);
            Header.NextAvailableHandle = nextHandle;

            return writer;
        }

        private void WriteSectionsAndClose(DxfWriter writer, IEnumerable<DxfSection> sections)
        {
            var writtenItems = new HashSet<IDxfItem>();
            var outputHandles = Header.Version >= DxfAcadVersion.R13 || Header.HandlesEnabled; // handles are always enabled on R13+
            foreach (var section in sections)
            {
                foreach (var pair in section.GetValuePairs(Header.Version, outputHandles, writtenItems))
                {
                    writer.WriteCodeValuePair(pair);
                }
            }

            writer.Close();
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal void WriteSingleSection(Stream stream, DxfSectionType sectionType)
        {
            var sections = Sections.Where(s => s.Type == sectionType);
            var writer = PrepareWriter(stream, asText: true);
            WriteSectionsAndClose(writer, sections);
        }

        internal IEnumerable<IDxfItemInternal> GetFileItems()
        {
            return this.TablesSection.GetTables(Header.Version).Cast<IDxfItemInternal>()
                .Concat(this.Blocks.Cast<IDxfItemInternal>())
                .Concat(this.Entities.Cast<IDxfItemInternal>())
                .Concat(this.Objects.Cast<IDxfItemInternal>())
                .Where(item => item != null);
        }

        public void Clear()
        {
            foreach (var section in Sections)
            {
                section.Clear();
            }
        }

        public void Normalize()
        {
            foreach (var table in TablesSection.GetAllTables())
            {
                table.Normalize();
            }

            BlocksSection.Normalize();
            ObjectsSection.Normalize();
        }
    }
}
