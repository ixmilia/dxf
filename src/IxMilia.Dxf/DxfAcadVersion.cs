// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf
{
    public enum DxfAcadVersion
    {
        Version_1_0,
        Version_1_2,
        Version_1_40,
        Version_2_05,
        Version_2_10,
        Version_2_21,
        Version_2_22,
        Version_2_5,
        Version_2_6,
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
        Min = Version_1_0,
        Max = R2013
    }

    public static class DxfAcadVersionStrings
    {
        private const string Version_1_0 = "MC0.0";
        private const string Version_1_2 = "AC1.2";
        private const string Version_1_40 = "AC1.40";
        private const string Version_2_05 = "AC1.50";
        private const string Version_2_10 = "AC2.10";
        private const string Version_2_21 = "AC2.21";
        private const string Version_2_22 = "AC2.22";
        private const string Version_2_22_Alternate = "AC1001";
        private const string Version_2_5 = "AC1002";
        private const string Version_2_6 = "AC1003";
        private const string R9 = "AC1004";
        private const string R10 = "AC1006";
        private const string R11 = "AC1009";
        private const string R12 = "AC1009";
        private const string R13_Pre = "AC1011"; // according to the DXF R13 spec, 'AC1011' is correct, but all later specs use 'AC1012'
        private const string R13 = "AC1012";
        private const string R14 = "AC1014";
        private const string R14_Alternate1 = "14";
        private const string R14_Alternate2 = "14.01";
        private const string R2000 = "AC1015";
        private const string R2000_Alternate1 = "15.0";
        private const string R2000_Alternate2 = "15.05"; // 2000i
        private const string R2000_Alternate3 = "15.06"; // 2002
        private const string R2004 = "AC1018";
        private const string R2004_Alternate1 = "16.0"; // 2004
        private const string R2004_Alternate2 = "16.1"; // 2005
        private const string R2004_Alternate3 = "16.2"; // 2006
        private const string R2007 = "AC1021";
        private const string R2007_Alternate1 = "17.0"; // 2007
        private const string R2007_Alternate2 = "17.1"; // 2008
        private const string R2007_Alternate3 = "17.2"; // 2009 (this is a guess)
        private const string R2010 = "AC1024";
        private const string R2010_Alternate1 = "18.0"; // 2010 (this is a guess)
        private const string R2010_Alternate2 = "18.1"; // 2011 (this is a guess)
        private const string R2010_Alternate3 = "18.2"; // 2012 (this is a guess)
        private const string R2013 = "AC1027";
        private const string R2013_Alternate1 = "19.0"; // 2013 (this is a guess)
        private const string R2013_Alternate2 = "19.1"; // 2014 (this is a guess)
        private const string R2013_Alternate3 = "19.2"; // 2015 (this is a guess)
        private const string R2013_Alternate4 = "19.3"; // 2016 (this is a guess)

        public static string VersionToString(DxfAcadVersion version)
        {
            switch (version)
            {
                case DxfAcadVersion.Version_1_0:
                    return Version_1_0;
                case DxfAcadVersion.Version_1_2:
                    return Version_1_2;
                case DxfAcadVersion.Version_1_40:
                    return Version_1_40;
                case DxfAcadVersion.Version_2_05:
                    return Version_2_05;
                case DxfAcadVersion.Version_2_10:
                    return Version_2_10;
                case DxfAcadVersion.Version_2_21:
                    return Version_2_21;
                case DxfAcadVersion.Version_2_22:
                    return Version_2_22; // also `Version_2_22_Alternate`; AC1001
                case DxfAcadVersion.Version_2_5:
                    return Version_2_5;
                case DxfAcadVersion.Version_2_6:
                    return Version_2_6;
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
                case Version_1_0:
                    return DxfAcadVersion.Version_1_0;
                case Version_1_2:
                    return DxfAcadVersion.Version_1_2;
                case Version_1_40:
                    return DxfAcadVersion.Version_1_40;
                case Version_2_05:
                    return DxfAcadVersion.Version_2_05;
                case Version_2_10:
                    return DxfAcadVersion.Version_2_10;
                case Version_2_21:
                    return DxfAcadVersion.Version_2_21;
                case Version_2_22:
                case Version_2_22_Alternate:
                    return DxfAcadVersion.Version_2_22;
                case Version_2_5:
                    return DxfAcadVersion.Version_2_5;
                case Version_2_6:
                    return DxfAcadVersion.Version_2_6;
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
                case R14_Alternate1:
                case R14_Alternate2:
                    return DxfAcadVersion.R14;
                case R2000:
                case R2000_Alternate1:
                case R2000_Alternate2:
                case R2000_Alternate3:
                    return DxfAcadVersion.R2000;
                case R2004:
                case R2004_Alternate1:
                case R2004_Alternate2:
                case R2004_Alternate3:
                    return DxfAcadVersion.R2004;
                case R2007:
                case R2007_Alternate1:
                case R2007_Alternate2:
                case R2007_Alternate3:
                    return DxfAcadVersion.R2007;
                case R2010:
                case R2010_Alternate1:
                case R2010_Alternate2:
                case R2010_Alternate3:
                    return DxfAcadVersion.R2010;
                case R2013:
                case R2013_Alternate1:
                case R2013_Alternate2:
                case R2013_Alternate3:
                case R2013_Alternate4:
                    return DxfAcadVersion.R2013;
                default:
                    throw new NotSupportedException($"The version string '{str}' was not expected");
            }
        }
    }
}
