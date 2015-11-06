// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IxMilia.Dxf
{
    internal static class DxfCommonConverters
    {
        public static string StringShort(short s)
        {
            return s.ToString(CultureInfo.InvariantCulture);
        }

        public static short StringShort(string s)
        {
            short result;
            short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
            return result;
        }

        public static bool BoolShort(short s)
        {
            return s != 0;
        }

        public static short BoolShort(bool b)
        {
            return (short)(b ? 1 : 0);
        }

        public static string GuidString(Guid g)
        {
            return g.ToString();
        }

        public static Guid GuidString(string s)
        {
            return new Guid(s);
        }

        public static uint UIntHandle(string s)
        {
            uint result;
            uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
            return result;
        }

        public static string UIntHandle(uint u)
        {
            return u.ToString("X", CultureInfo.InvariantCulture);
        }

        // the Dublin Julian date epoch is December 31, 1899, and defined as the Julian day 2415020
        // see http://en.wikipedia.org/wiki/Julian_day
        private static DateTime JulianDublinBase = new DateTime(1899, 12, 31, 0, 0, 0);
        private const double JulianDublinOffset = 2415020.0;

        public static DateTime DateDouble(double date)
        {
            if (date == 0.0)
                return JulianDublinBase;
            var daysFromDublin = date - JulianDublinOffset;
            return JulianDublinBase.AddDays(daysFromDublin);
        }

        public static double DateDouble(DateTime date)
        {
            var daysFromDublin = (date - JulianDublinBase).TotalDays;
            return JulianDublinOffset + daysFromDublin;
        }

        public static IEnumerable<string> SplitIntoLines(string text, int maxLineLength = 256)
        {
            var result = new List<string>();
            while (text.Length > maxLineLength)
            {
                result.Add(text.Substring(0, maxLineLength));
                text = text.Substring(maxLineLength);
            }

            if (text.Length > 0)
            {
                result.Add(text);
            }

            return result;
        }

        public static string HexBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            if (bytes != null)
            {
                foreach (var b in bytes)
                {
                    sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            return sb.ToString();
        }

        public static byte[] HexBytes(string s)
        {
            var buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
            {
                buffer[i / 2] = HexToByte(s[i], s[i + 1]);
            }

            return buffer;
        }

        public static byte HexToByte(char c1, char c2)
        {
            return (byte)((HexToByte(c1) << 4) + HexToByte(c2));
        }

        public static byte HexToByte(char c)
        {
            switch (c)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'a':
                case 'A': return 10;
                case 'b':
                case 'B': return 11;
                case 'c':
                case 'C': return 12;
                case 'd':
                case 'D': return 13;
                case 'e':
                case 'E': return 14;
                case 'f':
                case 'F': return 15;
                default:
                    return 0;
            }
        }
    }
}
