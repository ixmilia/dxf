using System;
using System.Collections.Generic;
using IxMilia.Dxf.Collections;
using IxMilia.Dxf.Entities;

namespace IxMilia.Dxf.Objects
{
    public enum DxfDictionaryDuplicateRecordHandling
    {
        NotApplicable = 0,
        KeepExisting = 1,
        UseClone = 2,
        UpdateXrefAndName = 3,
        UpdateName = 4,
        UnmangleName = 5
    }

    public enum DxfRotatedDimensionType
    {
        Parallel = 0,
        Perpendicular = 1
    }

    public enum DxfObjectOsnapType
    {
        None = 0,
        Endpoint = 1,
        Midpoint = 2,
        Center = 3,
        Node = 4,
        Quadrant = 5,
        Intersection = 6,
        Insertion = 7,
        Perpendicular = 8,
        Tangent = 9,
        Nearest = 10,
        ApparentIntersection = 11,
        Parallel = 12,
        StartPoint = 13
    }

    public enum DxfSubentityType
    {
        Edge = 1,
        Face = 2
    }

    public enum DxfGeoDataVersion
    {
        R2009 = 1,
        R2010 = 2
    }

    public enum DxfDesignCoordinateType
    {
        Unknown = 0,
        LocalGrid = 1,
        ProjectedGrid = 2,
        Geographic = 3
    }

    public enum DxfScaleEstimationMethod
    {
        None = 1,
        UserSpecified = 2,
        GridAtReferencePoint = 3,
        Prismoidal = 4
    }

    public enum DxfImageResolutionUnits
    {
        NoUnits = 0,
        Centimeters = 2,
        Inches = 5
    }

    public enum DxfRenderDuration
    {
        RenderByTime = 0,
        RenderByLevel = 1,
        UntilSatisfactory = 2
    }

    public enum DxfRenderAccuracy
    {
        Low = 0,
        Draft = 1,
        High = 2
    }

    public enum DxfSamplingFilter
    {
        Box = 0,
        Triangle = 1,
        Gauss = 2,
        Mitchell = 3,
        Lanczos = 4
    }

    public abstract partial class DxfObject : IDxfItem, IDxfHasXData
    {
        protected List<DxfCodePair> ExcessCodePairs = new List<DxfCodePair>();

        public IList<DxfCodePairGroup> ExtensionDataGroups { get; } = new ListNonNull<DxfCodePairGroup>();

        public IDictionary<string, DxfXDataApplicationItemCollection> XData { get; } = new DictionaryWithPredicate<string, DxfXDataApplicationItemCollection>((_key, value) => value != null);

        public abstract DxfObjectType ObjectType { get; }

        protected virtual DxfAcadVersion MinVersion
        {
            get { return DxfAcadVersion.Min; }
        }

        protected virtual DxfAcadVersion MaxVersion
        {
            get { return DxfAcadVersion.Max; }
        }

        protected DxfObject()
        {
            Initialize();
        }

        protected virtual void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
        }

        protected virtual DxfObject PostParse()
        {
            return this;
        }

        public IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
            var pairs = new List<DxfCodePair>();
            if (version >= MinVersion && version <= MaxVersion)
            {
                AddValuePairs(pairs, version, outputHandles, writeXData: true);
                AddTrailingCodePairs(pairs, version, outputHandles, writtenItems);
            }

            return pairs;
        }

        private void AddExtensionValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var group in ExtensionDataGroups)
            {
                group.AddValuePairs(pairs, version, outputHandles);
            }
        }

        internal virtual DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
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

                if (pair.Code == 0)
                {
                    break;
                }

                if (!TrySetPair(pair))
                {
                    ExcessCodePairs.Add(pair);
                }

                buffer.Advance();
            }

            return PostParse();
        }

        protected static bool BoolShort(short s)
        {
            return DxfCommonConverters.BoolShort(s);
        }

        protected static short BoolShort(bool b)
        {
            return DxfCommonConverters.BoolShort(b);
        }

        protected static DxfHandle HandleString(string s)
        {
            return DxfCommonConverters.HandleString(s);
        }

        protected static string HandleString(DxfHandle handle)
        {
            return DxfCommonConverters.HandleString(handle);
        }

        protected static DateTime DateDouble(double date)
        {
            return DxfCommonConverters.DateDouble(date);
        }

        protected static double DateDouble(DateTime date)
        {
            return DxfCommonConverters.DateDouble(date);
        }

        protected static DxfColor FromRawValue(short value)
        {
            return DxfColor.FromRawValue(value);
        }

        protected static short GetRawValue(DxfColor color)
        {
            return color?.RawValue ?? 0;
        }

        private static void SwallowObject(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                    break;
                buffer.Advance();
            }
        }
    }
}
