using System;
using System.Globalization;

namespace IxMilia.Dxf
{
    public struct DxfHandle : IEquatable<DxfHandle>
    {
        public ulong Value { get; }

        public DxfHandle(ulong value)
        {
            Value = value;
        }

        public static explicit operator ulong(DxfHandle handle) => handle.Value;

        public static explicit operator DxfHandle(ulong value) => new DxfHandle(value);

        public override string ToString()
        {
            return Value.ToString("X", CultureInfo.InvariantCulture);
        }

        public static bool TryParse(string s, out DxfHandle result)
        {
            var parseResult = ulong.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var handle);
            result = new DxfHandle(handle);
            return parseResult;
        }

        public override bool Equals(object obj)
        {
            return obj is DxfHandle handle && Equals(handle);
        }

        public bool Equals(DxfHandle other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(DxfHandle a, DxfHandle b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(DxfHandle a, DxfHandle b)
        {
            return !(a == b);
        }
    }
}
