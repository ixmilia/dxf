// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using IxMilia.Dxf.Sections;

namespace IxMilia.Dxf
{
    public partial class DxfCodePair
    {
        public const int CommentCode = 999;

        private KeyValuePair<int, object> data;

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
            get { return (bool)Value; }
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
    }
}
