using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Objects
{
    public class DxfSectionTypeSettings
    {
        public int SectionType { get; set; }
        public bool IsGenerationOption { get; set; }
        public IList<uint> SourceObjectHandles { get; } = new List<uint>();
        public uint DestinationObjectHandle { get; set; }
        public string DestinationFileName { get; set; }
        public IList<DxfSectionGeometrySettings> GeometrySettings { get; } = new ListNonNull<DxfSectionGeometrySettings>();

        internal void AddCodePairs(List<DxfCodePair> pairs)
        {
            pairs.Add(new DxfCodePair(1, "SectionTypeSettings"));
            pairs.Add(new DxfCodePair(90, SectionType));
            pairs.Add(new DxfCodePair(91, IsGenerationOption ? 1 : 0));
            pairs.Add(new DxfCodePair(92, SourceObjectHandles.Count));
            pairs.AddRange(SourceObjectHandles.Select(p => new DxfCodePair(330, DxfCommonConverters.UIntHandle(p))));
            pairs.Add(new DxfCodePair(331, DxfCommonConverters.UIntHandle(DestinationObjectHandle)));
            pairs.Add(new DxfCodePair(1, DestinationFileName));
            pairs.Add(new DxfCodePair(93, GeometrySettings.Count));
            pairs.Add(new DxfCodePair(2, "SectionGeometrySettings"));
            foreach (var geometry in GeometrySettings)
            {
                geometry.AddCodePairs(pairs);
            }

            pairs.Add(new DxfCodePair(3, "SectionTypeSettingsEnd"));
        }

        internal static DxfSectionTypeSettings FromBuffer(DxfCodePairBufferReader buffer)
        {
            if (buffer.Peek()?.Code == 0)
            {
                return null;
            }

            var settings = new DxfSectionTypeSettings();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                switch (pair.Code)
                {
                    case 1:
                        settings.DestinationFileName = pair.StringValue;
                        buffer.Advance();
                        break;
                    case 2:
                        Debug.Assert(pair.StringValue == "SectionGeometrySettings");
                        buffer.Advance();
                        for (var geometry = DxfSectionGeometrySettings.FromBuffer(buffer); geometry != null; geometry = DxfSectionGeometrySettings.FromBuffer(buffer))
                        {
                            settings.GeometrySettings.Add(geometry);
                        }
                        break;
                    case 3:
                        Debug.Assert(pair.StringValue == "SectionTypeSettingsEnd");
                        buffer.Advance();
                        break;
                    case 90:
                        settings.SectionType = pair.IntegerValue;
                        buffer.Advance();
                        break;
                    case 91:
                        settings.IsGenerationOption = pair.IntegerValue != 0;
                        buffer.Advance();
                        break;
                    case 92:
                        var sourceObjectsCount = pair.IntegerValue;
                        buffer.Advance();
                        break;
                    case 93:
                        var generationSettingsCount = pair.IntegerValue;
                        buffer.Advance();
                        break;
                    case 330:
                        settings.SourceObjectHandles.Add(DxfCommonConverters.UIntHandle(pair.StringValue));
                        buffer.Advance();
                        break;
                    case 331:
                        settings.DestinationObjectHandle = DxfCommonConverters.UIntHandle(pair.StringValue);
                        buffer.Advance();
                        break;
                    default:
                        return settings;
                }
            }

            return settings;
        }
    }

    public class DxfSectionGeometrySettings
    {
        public int SectionType { get; set; }
        public int GeometryCount { get; set; }
        public int BitFlags { get; set; }
        public DxfColor Color { get; set; } = DxfColor.ByBlock;
        public string LayerName { get; set; }
        public string LineTypeName { get; set; }
        public double LineTypeScale { get; set; } = 1.0;
        public string PlotStyleName { get; set; }
        public short LineWeight { get; set; }
        public short FaceTransparency { get; set; }
        public short EdgeTransparency { get; set; }
        public short HatchPatternType { get; set; }
        public string HatchPatternName { get; set; }
        public double HatchAngle { get; set; }
        public double HatchScale { get; set; } = 1.0;
        public double HatchSpacing { get; set; }

        internal void AddCodePairs(List<DxfCodePair> pairs)
        {
            pairs.Add(new DxfCodePair(90, SectionType));
            pairs.Add(new DxfCodePair(91, GeometryCount));
            pairs.Add(new DxfCodePair(92, BitFlags));
            pairs.Add(new DxfCodePair(63, Color?.RawValue ?? 0));
            pairs.Add(new DxfCodePair(8, LayerName));
            pairs.Add(new DxfCodePair(6, LineTypeName));
            pairs.Add(new DxfCodePair(40, LineTypeScale));
            pairs.Add(new DxfCodePair(1, PlotStyleName));
            pairs.Add(new DxfCodePair(370, LineWeight));
            pairs.Add(new DxfCodePair(70, FaceTransparency));
            pairs.Add(new DxfCodePair(71, EdgeTransparency));
            pairs.Add(new DxfCodePair(72, HatchPatternType));
            pairs.Add(new DxfCodePair(2, HatchPatternName));
            pairs.Add(new DxfCodePair(41, HatchAngle));
            pairs.Add(new DxfCodePair(42, HatchScale));
            pairs.Add(new DxfCodePair(43, HatchSpacing));
            pairs.Add(new DxfCodePair(3, "SectionGeometrySettingsEnd"));
        }

        internal static DxfSectionGeometrySettings FromBuffer(DxfCodePairBufferReader buffer)
        {
            var stillReading = true;
            var settings = new DxfSectionGeometrySettings();
            if (buffer.Peek()?.Code != 90)
            {
                // only code 90 can start one of these
                return null;
            }

            while (buffer.ItemsRemain && stillReading)
            {
                var pair = buffer.Peek();
                switch (pair.Code)
                {
                    case 1:
                        settings.PlotStyleName = pair.StringValue;
                        break;
                    case 2:
                        settings.HatchPatternName = pair.StringValue;
                        break;
                    case 3:
                        Debug.Assert(pair.StringValue == "SectionGeometrySettingsEnd");
                        stillReading = false;
                        break;
                    case 6:
                        settings.LineTypeName = pair.StringValue;
                        break;
                    case 8:
                        settings.LayerName = pair.StringValue;
                        break;
                    case 40:
                        settings.LineTypeScale = pair.DoubleValue;
                        break;
                    case 41:
                        settings.HatchAngle = pair.DoubleValue;
                        break;
                    case 42:
                        settings.HatchScale = pair.DoubleValue;
                        break;
                    case 43:
                        settings.HatchSpacing = pair.DoubleValue;
                        break;
                    case 63:
                        settings.Color = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 70:
                        settings.FaceTransparency = pair.ShortValue;
                        break;
                    case 71:
                        settings.EdgeTransparency = pair.ShortValue;
                        break;
                    case 72:
                        settings.HatchPatternType = pair.ShortValue;
                        break;
                    case 90:
                        settings.SectionType = pair.IntegerValue;
                        break;
                    case 91:
                        settings.GeometryCount = pair.IntegerValue;
                        break;
                    case 92:
                        settings.BitFlags = pair.IntegerValue;
                        break;
                    case 370:
                        settings.LineWeight = pair.ShortValue;
                        break;
                    default:
                        // unexpected end, return immediately without consuming the code pair
                        return settings;
                }

                buffer.Advance();
            }

            return settings;
        }
    }

    public partial class DxfSectionSettings
    {
        public IList<DxfSectionTypeSettings> SectionTypeSettings { get; } = new ListNonNull<DxfSectionTypeSettings>();

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, bool writeXData)
        {
            base.AddValuePairs(pairs, version, outputHandles, writeXData: false);
            pairs.Add(new DxfCodePair(100, "AcDbSectionSettings"));
            pairs.Add(new DxfCodePair(90, this.SectionType));
            pairs.Add(new DxfCodePair(91, SectionTypeSettings.Count));
            foreach (var settings in SectionTypeSettings)
            {
                settings.AddCodePairs(pairs);
            }

            if (writeXData)
            {
                DxfXData.AddValuePairs(XData, pairs, version, outputHandles);
            }
        }

        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                while (this.TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
                }

                switch (pair.Code)
                {
                    case 1:
                        Debug.Assert(pair.StringValue == "SectionTypeSettings");
                        buffer.Advance();
                        for (var sectionSettings = DxfSectionTypeSettings.FromBuffer(buffer); sectionSettings != null; sectionSettings = DxfSectionTypeSettings.FromBuffer(buffer))
                        {
                            SectionTypeSettings.Add(sectionSettings);
                        }
                        break;
                    case 90:
                        SectionType = pair.IntegerValue;
                        buffer.Advance();
                        break;
                    case 91:
                        var generationSettingsCount = pair.IntegerValue;
                        buffer.Advance();
                        break;
                    default:
                        if (!base.TrySetPair(pair))
                        {
                            ExcessCodePairs.Add(pair);
                        }

                        buffer.Advance();
                        break;
                }
            }

            return PostParse();
        }
    }
}
