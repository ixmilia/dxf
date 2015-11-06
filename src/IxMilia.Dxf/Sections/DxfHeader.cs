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

        private static string StringShort(short s)
        {
            return DxfCommonConverters.StringShort(s);
        }

        private static short StringShort(string s)
        {
            return DxfCommonConverters.StringShort(s);
        }

        private static bool BoolShort(short? s)
        {
            return DxfCommonConverters.BoolShort(s ?? 0);
        }

        private static short BoolShort(bool? b)
        {
            return DxfCommonConverters.BoolShort(b ?? false);
        }

        private static string GuidString(Guid g)
        {
            return DxfCommonConverters.GuidString(g);
        }

        private static Guid GuidString(string s)
        {
            return DxfCommonConverters.GuidString(s);
        }

        private static uint UIntHandle(string s)
        {
            return DxfCommonConverters.UIntHandle(s);
        }

        private static string UIntHandle(uint u)
        {
            return DxfCommonConverters.UIntHandle(u);
        }

        private static DateTime DateDouble(double date)
        {
            return DxfCommonConverters.DateDouble(date);
        }

        private static double DateDouble(DateTime date)
        {
            return DxfCommonConverters.DateDouble(date);
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
