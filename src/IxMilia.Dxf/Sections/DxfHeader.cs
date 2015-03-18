// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace IxMilia.Dxf
{
    public partial class DxfHeader
    {
        internal DxfHeader()
        {
            SetDefaults();
        }

        public bool IsViewportScaledToFit
        {
            get { return ViewportViewScaleFactor == 0.0; }
            set { ViewportViewScaleFactor = value ? 0.0 : 1.0; }
        }

        public object this[string variableName]
        {
            get { return GetValue(variableName); }
            set { SetValue(variableName, value); }
        }

        private static bool BoolShort(short s)
        {
            return s != 0;
        }

        private static short BoolShort(bool b)
        {
            return (short)(b ? 1 : 0);
        }

        private static string GuidString(Guid g)
        {
            return g.ToString();
        }

        private static Guid GuidString(string s)
        {
            return new Guid(s);
        }

        // the Dublin Julian date epoch is December 31, 1899, and defined as the Julian day 2415020
        // see http://en.wikipedia.org/wiki/Julian_day
        private static DateTime JulianDublinBase = new DateTime(1899, 12, 31, 0, 0, 0);
        private const double JulianDublinOffset = 2415020.0;

        private static DateTime DateDouble(double date)
        {
            if (date == 0.0)
                return JulianDublinBase;
            var daysFromDublin = date - JulianDublinOffset;
            return JulianDublinBase.AddDays(daysFromDublin);
        }

        private static double DateDouble(DateTime date)
        {
            var daysFromDublin = (date - JulianDublinBase).TotalDays;
            return JulianDublinOffset + daysFromDublin;
        }

        private static TimeSpan TimeSpanDouble(double d)
        {
            return TimeSpan.FromDays(d);
        }

        private static double TimeSpanDouble(TimeSpan t)
        {
            return t.TotalDays;
        }

        private static void EnsureCode(DxfCodePair pair, int code)
        {
            if (pair.Code != code)
            {
                Debug.Assert(false, string.Format("Expected code {0}, got {1}", code, pair.Code));
            }
        }

        private static void SetPoint(DxfCodePair pair, DxfPoint point)
        {
            switch (pair.Code)
            {
                case 10:
                    point.X = pair.DoubleValue;
                    break;
                case 20:
                    point.Y = pair.DoubleValue;
                    break;
                case 30:
                    point.Z = pair.DoubleValue;
                    break;
                default:
                    break;
            }
        }
    }
}
