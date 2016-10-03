// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf
{
    public enum DxfXDataType
    {
        String = 1000,
        ApplicationName = 1001,
        ControlString = 1002,
        Layer = 1003,
        BinaryData = 1004,
        Handle = 1005,
        RealTriple = 1010,
        WorldSpacePosition = 1011,
        WorldSpaceDisplacement = 1012,
        WorldDirection = 1013,
        Real = 1040,
        Distance = 1041,
        ScaleFactor = 1042,
        Integer = 1070,
        Long = 1071
    }

    public class DxfXData
    {
        public string ApplicationName { get; set; }
        public List<DxfXDataItem> Items { get; }

        public DxfXData(string applicationName, IEnumerable<DxfXDataItem> items)
        {
            ApplicationName = applicationName;
            Items = items.ToList();
        }

        internal void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            if (version >= DxfAcadVersion.R2000)
            {
                pairs.Add(new DxfCodePair((int)DxfXDataType.ApplicationName, ApplicationName));
                foreach (var item in Items)
                {
                    item.AddValuePairs(pairs, version, outputHandles);
                }
            }
        }

        internal static DxfXData FromBuffer(DxfCodePairBufferReader buffer, string applicationName)
        {
            var items = new List<DxfXDataItem>();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == (int)DxfXDataType.ApplicationName || pair.Code < 1000)
                {
                    // new xdata or non-xdata
                    break;
                }

                items.Add(DxfXDataItem.FromBuffer(buffer));
            }

            return new DxfXData(applicationName, items);
        }
    }

    public abstract class DxfXDataItem
    {
        public abstract DxfXDataType Type { get; }

        internal void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            var code = (int)Type;
            switch (Type)
            {
                case DxfXDataType.String:
                    pairs.Add(new DxfCodePair(code, ((DxfXDataString)this).Value));
                    break;
                case DxfXDataType.ControlString:
                    pairs.Add(new DxfCodePair(code, "{"));
                    foreach (var subItem in ((DxfXDataControlGroup)this).Items)
                    {
                        subItem.AddValuePairs(pairs, version, outputHandles);
                    }

                    pairs.Add(new DxfCodePair(code, "}"));
                    break;
                case DxfXDataType.Layer:
                    pairs.Add(new DxfCodePair(code, ((DxfXDataLayerName)this).Value));
                    break;
                case DxfXDataType.BinaryData:
                    pairs.Add(new DxfCodePair(code, DxfCommonConverters.HexBytes(((DxfXDataBinaryData)this).Value)));
                    break;
                case DxfXDataType.Handle:
                    pairs.Add(new DxfCodePair(code, DxfCommonConverters.UIntHandle(((DxfXDataHandle)this).Value)));
                    break;
                case DxfXDataType.RealTriple:
                    {
                        var point = ((DxfXData3Reals)this).Value;
                        pairs.Add(new DxfCodePair(code, point.X));
                        pairs.Add(new DxfCodePair(code + 10, point.Y));
                        pairs.Add(new DxfCodePair(code + 20, point.Z));
                        break;
                    }
                case DxfXDataType.WorldSpacePosition:
                    {
                        var point = ((DxfXDataWorldSpacePosition)this).Value;
                        pairs.Add(new DxfCodePair(code, point.X));
                        pairs.Add(new DxfCodePair(code + 10, point.Y));
                        pairs.Add(new DxfCodePair(code + 20, point.Z));
                        break;
                    }
                case DxfXDataType.WorldSpaceDisplacement:
                    {
                        var point = ((DxfXDataWorldSpaceDisplacement)this).Value;
                        pairs.Add(new DxfCodePair(code, point.X));
                        pairs.Add(new DxfCodePair(code + 10, point.Y));
                        pairs.Add(new DxfCodePair(code + 20, point.Z));
                        break;
                    }
                case DxfXDataType.WorldDirection:
                    {
                        var point = ((DxfXDataWorldDirection)this).Value;
                        pairs.Add(new DxfCodePair(code, point.X));
                        pairs.Add(new DxfCodePair(code + 10, point.Y));
                        pairs.Add(new DxfCodePair(code + 20, point.Z));
                        break;
                    }
                case DxfXDataType.Real:
                    pairs.Add(new DxfCodePair(code, ((DxfXDataReal)this).Value));
                    break;
                case DxfXDataType.Distance:
                    pairs.Add(new DxfCodePair(code, ((DxfXDataDistance)this).Value));
                    break;
                case DxfXDataType.ScaleFactor:
                    pairs.Add(new DxfCodePair(code, ((DxfXDataScaleFactor)this).Value));
                    break;
                case DxfXDataType.Integer:
                    pairs.Add(new DxfCodePair(code, ((DxfXDataInteger)this).Value));
                    break;
                case DxfXDataType.Long:
                    pairs.Add(new DxfCodePair(code, ((DxfXDataLong)this).Value));
                    break;
                default:
                    throw new InvalidOperationException("Unexpected XDATA item " + Type);
            }
        }

        internal static DxfXDataItem FromBuffer(DxfCodePairBufferReader buffer)
        {
            var pair = buffer.Peek();
            buffer.Advance();
            switch ((DxfXDataType)pair.Code)
            {
                case DxfXDataType.String:
                    return new DxfXDataString(pair.StringValue);
                case DxfXDataType.ControlString:
                    return DxfXDataControlGroup.ControlGroupFromBuffer(buffer);
                case DxfXDataType.Layer:
                    return new DxfXDataLayerName(pair.StringValue);
                case DxfXDataType.BinaryData:
                    return new DxfXDataBinaryData(DxfCommonConverters.HexBytes(pair.StringValue));
                case DxfXDataType.Handle:
                    return new DxfXDataHandle(DxfCommonConverters.UIntHandle(pair.StringValue));
                case DxfXDataType.RealTriple:
                    return new DxfXData3Reals(ReadPoint(buffer, pair.Code));
                case DxfXDataType.WorldSpacePosition:
                    return new DxfXDataWorldSpacePosition(ReadPoint(buffer, pair.Code));
                case DxfXDataType.WorldSpaceDisplacement:
                    return new DxfXDataWorldSpaceDisplacement(ReadPoint(buffer, pair.Code));
                case DxfXDataType.WorldDirection:
                    return new DxfXDataWorldDirection(ReadPoint(buffer, pair.Code));
                case DxfXDataType.Real:
                    return new DxfXDataReal(pair.DoubleValue);
                case DxfXDataType.Distance:
                    return new DxfXDataDistance(pair.DoubleValue);
                case DxfXDataType.ScaleFactor:
                    return new DxfXDataScaleFactor(pair.DoubleValue);
                case DxfXDataType.Integer:
                    return new DxfXDataInteger(pair.ShortValue);
                case DxfXDataType.Long:
                    return new DxfXDataLong(pair.IntegerValue);
                default:
                    throw new DxfReadException("Unexpected XDATA code pair", pair);
            }
        }

        private static DxfPoint ReadPoint(DxfCodePairBufferReader buffer, int expectedFirstCode)
        {
            // first value
            Debug.Assert(buffer.ItemsRemain);
            var pair = buffer.Peek();
            Debug.Assert(pair.Code == expectedFirstCode);
            var x = pair.DoubleValue;
            buffer.Advance();

            // second value
            Debug.Assert(buffer.ItemsRemain);
            pair = buffer.Peek();
            Debug.Assert(pair.Code == expectedFirstCode + 10);
            var y = pair.DoubleValue;
            buffer.Advance();

            // third value
            Debug.Assert(buffer.ItemsRemain);
            pair = buffer.Peek();
            Debug.Assert(pair.Code == expectedFirstCode + 20);
            var z = pair.DoubleValue;
            buffer.Advance();

            return new DxfPoint(x, y, z);
        }
    }

    public class DxfXDataString : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.String; } }

        public string Value { get; set; }

        public DxfXDataString(string value)
        {
            Value = value;
        }
    }

    public class DxfXDataControlGroup : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.ControlString; } }

        public IList<DxfXDataItem> Items { get; private set; }

        public DxfXDataControlGroup()
        {
            Items = new ListNonNull<DxfXDataItem>();
        }

        public DxfXDataControlGroup(IEnumerable<DxfXDataItem> items)
        {
            Items = items.ToList();
        }

        internal static DxfXDataControlGroup ControlGroupFromBuffer(DxfCodePairBufferReader buffer)
        {
            Debug.Assert(buffer.ItemsRemain);
            DxfCodePair pair;
            var items = new List<DxfXDataItem>();
            while (buffer.ItemsRemain)
            {
                pair = buffer.Peek();
                Debug.Assert(pair.Code >= 1000, "Unexpected non-XDATA code pair");
                if (pair.Code == (int)DxfXDataType.ControlString && pair.StringValue == "}")
                {
                    buffer.Advance();
                    break;
                }

                var item = DxfXDataItem.FromBuffer(buffer);
                items.Add(item);
            }

            return new DxfXDataControlGroup(items);
        }
    }

    public class DxfXDataLayerName : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.Layer; } }

        public string Value { get; set; }

        public DxfXDataLayerName(string value)
        {
            Value = value;
        }
    }

    public class DxfXDataBinaryData : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.BinaryData; } }

        public byte[] Value { get; set; }

        public DxfXDataBinaryData(byte[] value)
        {
            Value = value;
        }
    }

    public class DxfXDataHandle : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.Handle; } }

        public uint Value { get; set; }

        public DxfXDataHandle(uint value)
        {
            Value = value;
        }
    }

    public class DxfXData3Reals : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.RealTriple; } }

        public DxfPoint Value { get; set; }

        public DxfXData3Reals(DxfPoint value)
        {
            Value = value;
        }
    }

    public class DxfXDataWorldSpacePosition : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.WorldSpacePosition; } }

        public DxfPoint Value { get; set; }

        public DxfXDataWorldSpacePosition(DxfPoint value)
        {
            Value = value;
        }
    }

    public class DxfXDataWorldSpaceDisplacement : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.WorldSpaceDisplacement; } }

        public DxfPoint Value { get; set; }

        public DxfXDataWorldSpaceDisplacement(DxfPoint value)
        {
            Value = value;
        }
    }

    public class DxfXDataWorldDirection : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.WorldDirection; } }

        public DxfPoint Value { get; set; }

        public DxfXDataWorldDirection(DxfPoint value)
        {
            Value = value;
        }
    }

    public class DxfXDataReal : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.Real; } }

        public double Value { get; set; }

        public DxfXDataReal(double value)
        {
            Value = value;
        }
    }

    public class DxfXDataDistance : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.Distance; } }

        public double Value { get; set; }

        public DxfXDataDistance(double value)
        {
            Value = value;
        }
    }

    public class DxfXDataScaleFactor : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.ScaleFactor; } }

        public double Value { get; set; }

        public DxfXDataScaleFactor(double value)
        {
            Value = value;
        }
    }

    public class DxfXDataInteger : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.Integer; } }

        public short Value { get; set; }

        public DxfXDataInteger(short value)
        {
            Value = value;
        }
    }

    public class DxfXDataLong : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.Long; } }

        public int Value { get; set; }

        public DxfXDataLong(int value)
        {
            Value = value;
        }
    }
}
