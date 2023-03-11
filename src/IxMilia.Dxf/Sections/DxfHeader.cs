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

        public bool IsRestrictedVersion { get; set; } = false;

        private void SetManualDefaults()
        {
            IsRestrictedVersion = false;
        }

        private DxfAcadVersion VersionConverter(string str)
        {
            if (str.EndsWith("S"))
            {
                IsRestrictedVersion = true;
                str = str.Substring(0, str.Length - 1);
            }
            else
            {
                IsRestrictedVersion = false;
            }

            return DxfAcadVersionStrings.StringToVersion(str);
        }

        private string VersionConverter(DxfAcadVersion version)
        {
            var str = DxfAcadVersionStrings.VersionToString(version);
            if (IsRestrictedVersion)
            {
                str += "S";
            }

            return str;
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

        private static DxfHandle HandleString(string s)
        {
            return DxfCommonConverters.HandleString(s);
        }

        private static string HandleString(DxfHandle handle)
        {
            return DxfCommonConverters.HandleString(handle);
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

        private static string DefaultIfNullOrEmpty(string value, string defaultValue)
        {
            return DxfCommonConverters.DefaultIfNullOrEmpty(value, defaultValue);
        }

        private static double EnsurePositiveOrDefault(double value, double defaultValue)
        {
            return DxfCommonConverters.EnsurePositiveOrDefault(value, defaultValue);
        }

        private static void EnsureCode(DxfCodePair pair, int code)
        {
            if (pair.Code != code)
            {
                Debug.Assert(false, string.Format("Expected code {0}, got {1}", code, pair.Code));
            }
        }

        private static DxfPoint UpdatePoint(DxfCodePair pair, DxfPoint point)
        {
            switch (pair.Code)
            {
                case 10:
                    return point.WithUpdatedX(pair.DoubleValue);
                case 20:
                    return point.WithUpdatedY(pair.DoubleValue);
                case 30:
                    return point.WithUpdatedZ(pair.DoubleValue);
                default:
                    return point;
            }
        }
    }
}
