// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using IxMilia.Dxf.Sections;

namespace IxMilia.Dxf
{
    public partial class DxfCodePair : DxfCodePairOrGroup
    {
        public const int CommentCode = 999;

        private KeyValuePair<int, object> data;

        public bool IsCodePair { get { return true; } }

        public int Code
        {
            get { return data.Key; }
            set { data = new KeyValuePair<int, object>(value, data.Value); }
        }

        public object Value
        {
            get { return data.Value; }
            set { data = new KeyValuePair<int, object>(data.Key, value); }
        }

        public string StringValue
        {
            get { return (string)Value; }
        }

        public double DoubleValue
        {
            get { return (double)Value; }
        }

        public short ShortValue
        {
            get { return (short)Value; }
        }

        public int IntegerValue
        {
            get { return (int)Value; }
        }

        public long LongValue
        {
            get { return (long)Value; }
        }

        public bool BoolValue
        {
            get
            {
                // some intances of code 290 are actually shorts
                return IsPotentialShortAsBool(Code)
                    ? (short)Value != 0
                    : (bool)Value;
            }
        }

        public DxfCodePair(int code, string value)
        {
            Debug.Assert(ExpectedType(code) == typeof(string));
            data = new KeyValuePair<int, object>(code, value);
        }

        public DxfCodePair(int code, double value)
        {
            Debug.Assert(ExpectedType(code) == typeof(double));
            data = new KeyValuePair<int, object>(code, value);
        }

        public DxfCodePair(int code, short value)
        {
            // some code pairs in the spec expect code 290 shorts even though the spec says code 290
            // should really be a bool
            if (!IsPotentialShortAsBool(code))
                Debug.Assert(ExpectedType(code) == typeof(short));
            data = new KeyValuePair<int, object>(code, value);
        }

        public DxfCodePair(int code, int value)
        {
            Debug.Assert(ExpectedType(code) == typeof(int));
            data = new KeyValuePair<int, object>(code, value);
        }

        public DxfCodePair(int code, long value)
        {
            Debug.Assert(ExpectedType(code) == typeof(long));
            data = new KeyValuePair<int, object>(code, value);
        }

        public DxfCodePair(int code, bool value)
        {
            Debug.Assert(ExpectedType(code) == typeof(bool));
            data = new KeyValuePair<int, object>(code, value);
        }

        internal DxfCodePair(int code, object value)
        {
            // internal for specific cases where the type isn't known
            data = new KeyValuePair<int, object>(code, value);
        }

        internal static bool IsPotentialShortAsBool(int code)
        {
            return code >= 290 && code <= 299;
        }

        private bool IsHandle
        {
            get
            {
                return handleRegex.IsMatch(StringValue);
            }
        }

        private static Regex handleRegex = new Regex("([a-fA-F0-9]){1,16}");

        public override string ToString()
        {
            return string.Format("[{0}: {1}]", Code, Value);
        }

        public static bool IsSectionStart(DxfCodePair pair)
        {
            return pair.Code == 0 && pair.StringValue == DxfSection.SectionText;
        }

        public static bool IsSectionEnd(DxfCodePair pair)
        {
            return pair.Code == 0 && pair.StringValue == DxfSection.EndSectionText;
        }

        public static bool IsEof(DxfCodePair pair)
        {
            return pair.Code == 0 && pair.StringValue == DxfFile.EofText;
        }

        public static bool IsComment(DxfCodePair pair)
        {
            return pair.Code == CommentCode;
        }

        public static bool operator ==(DxfCodePair a, DxfCodePair b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (((object)a) == null || ((object)b) == null)
                return false;
            return a.Code == b.Code && a.Value.Equals(b.Value);
        }

        public static bool operator !=(DxfCodePair a, DxfCodePair b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            var hash = Code.GetHashCode();
            if (Value != null)
            {
                hash ^= Value.GetHashCode();
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj is DxfCodePair)
                return this == (DxfCodePair)obj;
            return false;
        }
    }
}
