// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf
{
    public enum DxfAcadVersion
    {
        R9,
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
        Min = R9,
        Max = R2013
    }

    public static class DxfAcadVersionStrings
    {
        private const string R9 = "AC1004";
        private const string R10 = "AC1006";
        private const string R11 = "AC1009";
        private const string R12 = "AC1009";
        private const string R13_Pre = "AC1011"; // according to the DXF R13 spec, 'AC1011' is correct, but all later specs use 'AC1012'
        private const string R13 = "AC1012";
        private const string R14 = "AC1014";
        private const string R2000 = "AC1015";
        private const string R2004 = "AC1018";
        private const string R2007 = "AC1021";
        private const string R2010 = "AC1024";
        private const string R2013 = "AC1027";

        public static string VersionToString(DxfAcadVersion version)
        {
            switch (version)
            {
                case DxfAcadVersion.R9:
                    return R9;
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
                case R9:
                    return DxfAcadVersion.R9;
                case R10:
                    return DxfAcadVersion.R10;
                case R11:
                // case R12:
                    return DxfAcadVersion.R12;
                case R13_Pre:
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
                    throw new NotSupportedException($"The version string '{str}' was not expected");
            }
        }
    }
}
