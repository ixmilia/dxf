// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

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

    public abstract partial class DxfObject
    {
        protected List<DxfCodePair> ExcessCodePairs = new List<DxfCodePair>();
        protected DxfXData XDataProtected { get; set; }
        public List<DxfCodePairGroup> ExtensionDataGroups { get; private set; }

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
            ExtensionDataGroups = new List<DxfCodePairGroup>();
        }

        protected virtual void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
        }

        protected virtual DxfObject PostParse()
        {
            return this;
        }

        public IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version, bool outputHandles)
        {
            var pairs = new List<DxfCodePair>();
            if (version >= MinVersion && version <= MaxVersion)
            {
                AddValuePairs(pairs, version, outputHandles);
                AddTrailingCodePairs(pairs, version, outputHandles);
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

        internal virtual bool TrySetExtensionData(DxfCodePair pair, DxfCodePairBufferReader buffer)
        {
            if (pair.Code == DxfCodePairGroup.GroupCodeNumber)
            {
                buffer.Advance();
                var groupName = pair.StringValue;
                DxfCodePairGroup group;
                if (DxfCodePairGroup.IsSingletonGroup(groupName))
                {
                    groupName = DxfCodePairGroup.GetGroupName(groupName);
                    group = DxfCodePairGroup.FromBuffer(buffer, groupName);
                }
                else
                {
                    // single-value group
                    group = DxfCodePairGroup.CreateSingletonGroup(groupName, buffer.Peek());
                    buffer.Advance();
                }

                ExtensionDataGroups.Add(group);
                return true;
            }
            else if (pair.Code == (int)DxfXDataType.ApplicationName)
            {
                XDataProtected = DxfXData.FromBuffer(buffer, pair.StringValue);
                return true;
            }

            return false;
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

                if (TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
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

        protected static bool NotBoolShort(short s)
        {
            return !BoolShort(s);
        }

        protected static short NotBoolShort(bool b)
        {
            return BoolShort(!b);
        }

        protected static uint UIntHandle(string s)
        {
            return DxfCommonConverters.UIntHandle(s);
        }

        protected static string UIntHandle(uint u)
        {
            return DxfCommonConverters.UIntHandle(u);
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
