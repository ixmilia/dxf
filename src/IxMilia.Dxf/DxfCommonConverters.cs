// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

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
            short.TryParse(s, out result);
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
    }
}
