// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This line is required for T4 template generation to work. <#+

#if DXF
using System;

namespace IxMilia.Dxf
{
#endif
    public partial class DxfCodePair
    {
        public static Type ExpectedType(int code)
        {
            Type expected = typeof(string);
            Func<int, int, bool> between = (lower, upper) => code >= lower && code <= upper;

            if (between(0, 9))
                expected = typeof(string);
            else if (between(10, 39))
                expected = typeof(double);
            else if (between(40, 59))
                expected = typeof(double);
            else if (between(60, 79))
                expected = typeof(short);
            else if (between(90, 99))
                expected = typeof(int);
            else if (code == 100)
                expected = typeof(string);
            else if (code == 102)
                expected = typeof(string);
            else if (code == 105)
                expected = typeof(string);
            else if (between(110, 119))
                expected = typeof(double);
            else if (between(120, 129))
                expected = typeof(double);
            else if (between(130, 139))
                expected = typeof(double);
            else if (between(140, 149))
                expected = typeof(double);
            else if (between(160, 169))
                expected = typeof(long);
            else if (between(170, 179))
                expected = typeof(short);
            else if (between(210, 239))
                expected = typeof(double);
            else if (between(270, 279))
                expected = typeof(short);
            else if (between(280, 289))
                expected = typeof(short);
            else if (between(290, 299))
                expected = typeof(bool);
            else if (between(300, 309))
                expected = typeof(string);
            else if (between(310, 319))
                expected = typeof(string);
            else if (between(320, 329))
                expected = typeof(string);
            else if (between(330, 369))
                expected = typeof(string);
            else if (between(370, 379))
                expected = typeof(short);
            else if (between(380, 389))
                expected = typeof(double);
            else if (between(390, 399))
                expected = typeof(string);
            else if (between(400, 409))
                expected = typeof(short);
            else if (between(410, 419))
                expected = typeof(string);
            else if (between(420, 429))
                expected = typeof(int);
            else if (between(430, 439))
                expected = typeof(string);
            else if (between(440, 449))
                expected = typeof(int);
            else if (between(450, 459))
                expected = typeof(long);
            else if (between(460, 469))
                expected = typeof(double);
            else if (between(470, 479))
                expected = typeof(string);
            else if (between(480, 481))
                expected = typeof(string);
            else if (code == 999)
                expected = typeof(string);
            else if (between(1000, 1009))
                expected = typeof(string);
            else if (between(1010, 1059))
                expected = typeof(double);
            else if (between(1060, 1070))
                expected = typeof(short);
            else if (code == 1071)
                expected = typeof(int);
            else
                throw new DxfReadException("Unknown type code " + code);

            return expected;
        }
    }
#if DXF
}
#endif

// This line is required for T4 template generation to work. #>
