using System;
using System.Collections;
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

    internal static class DxfXData
    {
        internal static void AddValuePairs(IDictionary<string, DxfXDataApplicationItemCollection> xdata, List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            if (version >= DxfAcadVersion.R14)
            {
                foreach (var applicationGroup in xdata)
                {
                    pairs.Add(new DxfCodePair(1001, applicationGroup.Key));
                    foreach (var item in applicationGroup.Value)
                    {
                        item.AddValuePairs(pairs, version, outputHandles);
                    }
                }
            }
        }

        internal static void PopulateFromBuffer(DxfCodePairBufferReader buffer, IDictionary<string, DxfXDataApplicationItemCollection> xdata, string applicationName)
        {
            xdata[applicationName] = new DxfXDataApplicationItemCollection();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == (int)DxfXDataType.ApplicationName || pair.Code < 1000)
                {
                    return;
                }

                var item = DxfXDataItem.FromBuffer(buffer);
                if (item != null)
                {
                    xdata[applicationName].Add(item);
                }
            }
        }

        internal static void CopyItemsTo(this IDictionary<string, DxfXDataApplicationItemCollection> source, IDictionary<string, DxfXDataApplicationItemCollection> destination)
        {
            foreach (var item in source)
            {
                destination[item.Key] = item.Value;
            }
        }
    }

    public class DxfXDataApplicationItemCollection : IList<DxfXDataItem>
    {
        private IList<DxfXDataItem> _items = new ListNonNull<DxfXDataItem>();

        public DxfXDataApplicationItemCollection(IEnumerable<DxfXDataItem> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public DxfXDataApplicationItemCollection(params DxfXDataItem[] items)
            : this((IEnumerable<DxfXDataItem>)items)
        {
        }

        public DxfXDataItem this[int index] { get => _items[index]; set => _items[index] = value; }

        public int Count => _items.Count;

        public bool IsReadOnly => _items.IsReadOnly;

        public void Add(DxfXDataItem item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(DxfXDataItem item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(DxfXDataItem[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<DxfXDataItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int IndexOf(DxfXDataItem item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, DxfXDataItem item)
        {
            _items.Insert(index, item);
        }

        public bool Remove(DxfXDataItem item)
        {
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }

    public abstract class DxfXDataItem
    {
        public abstract DxfXDataType Type { get; }

        internal void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            var code = (int)Type;
            switch (this)
            {
                case DxfXDataString s:
                    pairs.Add(new DxfCodePair(code, s.Value));
                    break;
                case DxfXDataItemList l:
                    AddListValuePairs(l.Items, pairs, version, outputHandles);
                    break;
                case DxfXDataLayerName l:
                    pairs.Add(new DxfCodePair(code, l.Value));
                    break;
                case DxfXDataBinaryData b:
                    pairs.Add(new DxfCodePair(code, DxfCommonConverters.HexBytes(b.Value)));
                    break;
                case DxfXDataHandle h:
                    pairs.Add(new DxfCodePair(code, DxfCommonConverters.HandleString(h.Value)));
                    break;
                case DxfXData3Reals r:
                    pairs.Add(new DxfCodePair(code, r.Value.X));
                    pairs.Add(new DxfCodePair(code + 10, r.Value.Y));
                    pairs.Add(new DxfCodePair(code + 20, r.Value.Z));
                    break;
                case DxfXDataWorldSpacePosition w:
                    pairs.Add(new DxfCodePair(code, w.Value.X));
                    pairs.Add(new DxfCodePair(code + 10, w.Value.Y));
                    pairs.Add(new DxfCodePair(code + 20, w.Value.Z));
                    break;
                case DxfXDataWorldSpaceDisplacement w:
                    pairs.Add(new DxfCodePair(code, w.Value.X));
                    pairs.Add(new DxfCodePair(code + 10, w.Value.Y));
                    pairs.Add(new DxfCodePair(code + 20, w.Value.Z));
                    break;
                case DxfXDataWorldDirection w:
                    pairs.Add(new DxfCodePair(code, w.Value.X));
                    pairs.Add(new DxfCodePair(code + 10, w.Value.Y));
                    pairs.Add(new DxfCodePair(code + 20, w.Value.Z));
                    break;
                case DxfXDataReal r:
                    pairs.Add(new DxfCodePair(code, r.Value));
                    break;
                case DxfXDataDistance d:
                    pairs.Add(new DxfCodePair(code, d.Value));
                    break;
                case DxfXDataScaleFactor s:
                    pairs.Add(new DxfCodePair(code, s.Value));
                    break;
                case DxfXDataInteger i:
                    pairs.Add(new DxfCodePair(code, i.Value));
                    break;
                case DxfXDataLong l:
                    pairs.Add(new DxfCodePair(code, l.Value));
                    break;
                default:
                    throw new InvalidOperationException("Unexpected XDATA item " + Type);
            }
        }

        private static void AddListValuePairs(IEnumerable<DxfXDataItem> items, List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            pairs.Add(new DxfCodePair((int)DxfXDataType.ControlString, "{"));
            foreach (var subItem in items)
            {
                subItem.AddValuePairs(pairs, version, outputHandles);
            }

            pairs.Add(new DxfCodePair((int)DxfXDataType.ControlString, "}"));
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
                    return DxfXDataItemList.ListFromBuffer(buffer);
                case DxfXDataType.Layer:
                    return new DxfXDataLayerName(pair.StringValue);
                case DxfXDataType.BinaryData:
                    return new DxfXDataBinaryData(DxfCommonConverters.HexBytes(pair.StringValue));
                case DxfXDataType.Handle:
                    return new DxfXDataHandle(DxfCommonConverters.HandleString(pair.StringValue));
                case DxfXDataType.RealTriple:
                    return new DxfXData3Reals(ReadPoint(pair, buffer, pair.Code));
                case DxfXDataType.WorldSpacePosition:
                    return new DxfXDataWorldSpacePosition(ReadPoint(pair, buffer, pair.Code));
                case DxfXDataType.WorldSpaceDisplacement:
                    return new DxfXDataWorldSpaceDisplacement(ReadPoint(pair, buffer, pair.Code));
                case DxfXDataType.WorldDirection:
                    return new DxfXDataWorldDirection(ReadPoint(pair, buffer, pair.Code));
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
                    return null; // unexpected XDATA code pair
            }
        }

        private static DxfPoint ReadPoint(DxfCodePair xCoord, DxfCodePairBufferReader buffer, int expectedFirstCode)
        {
            // first value
            var pair = xCoord;
            Debug.Assert(pair.Code == expectedFirstCode);
            var x = pair.DoubleValue;

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

    public class DxfXDataItemList : DxfXDataItem
    {
        public override DxfXDataType Type { get { return DxfXDataType.ControlString; } }

        public IList<DxfXDataItem> Items { get; private set; }

        public DxfXDataItemList()
        {
            Items = new ListNonNull<DxfXDataItem>();
        }

        public DxfXDataItemList(IEnumerable<DxfXDataItem> items)
        {
            Items = items.ToList();
        }

        public DxfXDataItemList(params DxfXDataItem[] items)
            : this((IEnumerable<DxfXDataItem>)items)
        {
        }

        internal static DxfXDataItemList ListFromBuffer(DxfCodePairBufferReader buffer)
        {
            Debug.Assert(buffer.ItemsRemain);
            DxfCodePair pair;
            var items = new List<DxfXDataItem>();
            while (buffer.ItemsRemain)
            {
                pair = buffer.Peek();
                if (pair.Code == (int)DxfXDataType.ControlString && pair.StringValue == "}")
                {
                    buffer.Advance();
                    break;
                }

                var item = DxfXDataItem.FromBuffer(buffer);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return new DxfXDataItemList(items);
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

        public DxfHandle Value { get; set; }

        public DxfXDataHandle(DxfHandle value)
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
