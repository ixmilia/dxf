using System;

namespace IxMilia.Dxf
{
    public enum DxfAcadVersion
    {
        R10,
        R11,
        R12,
        R13,
        R14,
        R2000,
        R2004,
        R2007,
        R2010,
        R2013,
        Min = R10,
        Max = R2013
    }

    public static class DxfAcadVersionStrings
    {
        public const string R10 = "AC1006";
        public const string R11 = "AC1009";
        public const string R12 = "AC1009";
        public const string R13 = "AC1012";
        public const string R14 = "AC1014";
        public const string R2000 = "AC1015";
        public const string R2004 = "AC1018";
        public const string R2007 = "AC1021";
        public const string R2010 = "AC1024";
        public const string R2013 = "AC1027";

        public static string VersionToString(DxfAcadVersion version)
        {
            switch (version)
            {
                case DxfAcadVersion.R10:
                    return R10;
                case DxfAcadVersion.R11:
                    return R11;
                case DxfAcadVersion.R12:
                    return R12;
                case DxfAcadVersion.R13:
                    return R13;
                case DxfAcadVersion.R14:
                    return R14;
                case DxfAcadVersion.R2000:
                    return R2000;
                case DxfAcadVersion.R2004:
                    return R2004;
                case DxfAcadVersion.R2007:
                    return R2007;
                case DxfAcadVersion.R2010:
                    return R2010;
                case DxfAcadVersion.R2013:
                    return R2013;
                default:
                    throw new NotSupportedException();
            }
        }

        public static DxfAcadVersion StringToVersion(string str)
        {
            switch (str)
            {
                case R10:
                    return DxfAcadVersion.R10;
                case R11:
                // case R12:
                    return DxfAcadVersion.R12;
                case R13:
                    return DxfAcadVersion.R13;
                case R14:
                    return DxfAcadVersion.R14;
                case R2000:
                    return DxfAcadVersion.R2000;
                case R2004:
                    return DxfAcadVersion.R2004;
                case R2007:
                    return DxfAcadVersion.R2007;
                case R2010:
                    return DxfAcadVersion.R2010;
                case R2013:
                    return DxfAcadVersion.R2013;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
