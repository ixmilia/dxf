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

        public DxfViewPort ActiveViewPort
        {
            get
            {
                // prefer the view port named `*ACTIVE` then fall back to the first available one
                return ViewPorts.FirstOrDefault(v => string.Compare(v.Name, DxfViewPort.ActiveViewPortName, StringComparison.OrdinalIgnoreCase) == 0)
                    ?? ViewPorts.FirstOrDefault();
            }
            set
            {
                // replace `*ACTIVE`, ensuring the name is correct
                if (string.Compare(value.Name, DxfViewPort.ActiveViewPortName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    value.Name = DxfViewPort.ActiveViewPortName;
                }

                for (int i = 0; i < ViewPorts.Count; i++)
                {
                    if (string.Compare(ViewPorts[i].Name, DxfViewPort.ActiveViewPortName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        ViewPorts[i] = value;
                        return;
                    }
                }

                // `*ACTIVE` couldn't be found, just add it on the end
                ViewPorts.Add(value);
            }
        }

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

        public DxfBoundingBox GetBoundingBox()
        {
            var boundingBoxes = Entities.Select(e => e.GetBoundingBox()).Where(b => b != null).Select(b => b.GetValueOrDefault()).ToList();
            return boundingBoxes.Count == 0
                ? default(DxfBoundingBox)
                : boundingBoxes.Skip(1).Aggregate(boundingBoxes[0], (box1, box2) => box1.Combine(box2));
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
            SetExtents();
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

        private void SetExtents()
        {
            var boundingBox = GetBoundingBox();
            Header.MinimumDrawingExtents = boundingBox.MinimumPoint;
            Header.MaximumDrawingExtents = boundingBox.MaximumPoint;
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

                if (section is DxfEntitiesSection)
                {
                    var entitiesSection = (DxfEntitiesSection)section;
                    var addedObjects = new HashSet<DxfObject>(Objects);
                    foreach (var additionalObject in entitiesSection.AdditionalObjects)
                    {
                        if (additionalObject != null && addedObjects.Add(additionalObject))
                        {
                            Objects.Add(additionalObject);
                        }
                    }
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
            EnsureStyleObjects();
            EnsureTableItems();
            foreach (var table in TablesSection.GetAllTables())
            {
                table.Normalize();
            }

            BlocksSection.Normalize();
            ObjectsSection.Normalize();
        }

        private void EnsureStyleObjects()
        {
            var existingMLineStyles = GenerateHashSet(Objects.OfType<DxfMLineStyle>().Select(m => m.StyleName));
            AddMissingItems(existingMLineStyles, Entities.OfType<DxfMLine>().Select(m => m.StyleName), name => Objects.Add(new DxfMLineStyle() { StyleName = name }));
        }

        private void EnsureTableItems()
        {
            var existingDimStyles = GetExistingNames(DimensionStyles);
            AddMissingDimensionStyles(existingDimStyles, new[] { Header.DimensionStyleName });
            AddMissingDimensionStyles(existingDimStyles, Entities.OfType<DxfDimensionBase>().Select(d => d.DimensionStyleName));
            AddMissingDimensionStyles(existingDimStyles, Entities.OfType<DxfLeader>().Select(d => d.DimensionStyleName));
            AddMissingDimensionStyles(existingDimStyles, Entities.OfType<DxfTolerance>().Select(d => d.DimensionStyleName));

            var existingLayers = GetExistingNames(Layers);
            AddMissingLayers(existingLayers, new[] { Header.CurrentLayer });
            AddMissingLayers(existingLayers, Blocks.Select(b => b.Layer));
            AddMissingLayers(existingLayers, Blocks.SelectMany(b => b.Entities.Select(e => e.Layer)));
            AddMissingLayers(existingLayers, Entities.Select(e => e.Layer));
            AddMissingLayers(existingLayers, Objects.OfType<DxfLayerFilter>().SelectMany(l => l.LayerNames));
            AddMissingLayers(existingLayers, Objects.OfType<DxfLayerIndex>().SelectMany(l => l.LayerNames));

            var existingLineTypes = GetExistingNames(LineTypes);
            AddMissingLineTypes(existingLineTypes, new[] { Header.CurrentEntityLineType, Header.DimensionLineType });
            AddMissingLineTypes(existingLineTypes, Layers.Select(l => l.LineTypeName));
            AddMissingLineTypes(existingLineTypes, Blocks.SelectMany(b => b.Entities.Select(e => e.LineTypeName)));
            AddMissingLineTypes(existingLineTypes, Entities.Select(e => e.LineTypeName));
            AddMissingLineTypes(existingLineTypes, Objects.OfType<DxfMLineStyle>().SelectMany(m => m.Elements.Select(e => e.LineType)));

            var existingStyles = GetExistingNames(Styles);
            AddMissingStyles(existingStyles, Entities.OfType<DxfArcAlignedText>().Select(a => a.TextStyleName));
            AddMissingStyles(existingStyles, Entities.OfType<DxfAttribute>().Select(a => a.TextStyleName));
            AddMissingStyles(existingStyles, Entities.OfType<DxfAttributeDefinition>().Select(a => a.TextStyleName));
            AddMissingStyles(existingStyles, Entities.OfType<DxfMText>().Select(m => m.TextStyleName));
            AddMissingStyles(existingStyles, Entities.OfType<DxfText>().Select(t => t.TextStyleName));
            AddMissingStyles(existingStyles, Objects.OfType<DxfMLineStyle>().Select(m => m.StyleName));

            var existingViews = GetExistingNames(Views);
            AddMissingViews(existingViews, Objects.OfType<DxfPlotSettings>().Select(p => p.PlotViewName));

            var existingUcs = GetExistingNames(UserCoordinateSystems);
            AddMissingUcs(existingUcs, new[] {
                Header.UCSDefinitionName,
                Header.UCSName,
                Header.OrthoUCSReference,
                Header.PaperspaceUCSDefinitionName,
                Header.PaperspaceUCSName,
                Header.PaperspaceOrthoUCSReference,
            });

            // don't need to do anything special for AppIds, BlockRecords, or ViewPorts
        }

        private static HashSet<string> GenerateHashSet(IEnumerable<string> items)
        {
            return new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<string> GetExistingNames(IEnumerable<DxfSymbolTableFlags> items)
        {
            return GenerateHashSet(items.Select(i => i.Name));
        }

        private void AddMissingDimensionStyles(HashSet<string> existingDimensionStyles, IEnumerable<string> dimensionStylesToAdd)
        {
            AddMissingTableItems<DxfDimStyle>(existingDimensionStyles, dimensionStylesToAdd, ds => DimensionStyles.Add(ds));
        }

        private void AddMissingLayers(HashSet<string> existingLayers, IEnumerable<string> layersToAdd)
        {
            AddMissingTableItems<DxfLayer>(existingLayers, layersToAdd, l => Layers.Add(l));
        }

        private void AddMissingLineTypes(HashSet<string> existingLineTypes, IEnumerable<string> lineTypesToAdd)
        {
            AddMissingTableItems<DxfLineType>(existingLineTypes, lineTypesToAdd, lt => LineTypes.Add(lt));
        }

        private void AddMissingStyles(HashSet<string> existingStyles, IEnumerable<string> stylesToAdd)
        {
            AddMissingTableItems<DxfStyle>(existingStyles, stylesToAdd, s => Styles.Add(s));
        }

        private void AddMissingViews(HashSet<string> existingViews, IEnumerable<string> viewsToAdd)
        {
            AddMissingTableItems<DxfView>(existingViews, viewsToAdd, v => Views.Add(v));
        }

        private void AddMissingUcs(HashSet<string> existingUcs, IEnumerable<string> ucsToAdd)
        {
            AddMissingTableItems<DxfUcs>(existingUcs, ucsToAdd, u => UserCoordinateSystems.Add(u));
        }

        private static void AddMissingItems(HashSet<string> existingItems, IEnumerable<string> itemsToAdd, Action<string> addItem)
        {
            foreach (var itemToAdd in itemsToAdd)
            {
                if (itemToAdd != null && !existingItems.Contains(itemToAdd))
                {
                    addItem(itemToAdd);
                    existingItems.Add(itemToAdd);
                }
            }
        }

        private static void AddMissingTableItems<T>(HashSet<string> existingItems, IEnumerable<string> itemsToAdd, Action<T> addItem)
            where T: DxfSymbolTableFlags, new()
        {
            AddMissingItems(existingItems, itemsToAdd, name => addItem(new T() { Name = name }));
        }
    }
}
