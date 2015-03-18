// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace IxMilia.Dxf
{
    public struct DxfProxyCapabilities
    {
        private int Flags;
        internal int Value { get { return Flags; } }

        internal DxfProxyCapabilities(int flags)
            : this()
        {
            Flags = flags;
        }

        public bool IsEraseAllowed
        {
            get { return DxfHelpers.GetFlag(Flags, 1); }
            set { DxfHelpers.SetFlag(value, ref Flags, 1); }
        }

        public bool IsTransformAllowed
        {
            get { return DxfHelpers.GetFlag(Flags, 2); }
            set { DxfHelpers.SetFlag(value, ref Flags, 2); }
        }

        public bool IsColorChangeAllowed
        {
            get { return DxfHelpers.GetFlag(Flags, 4); }
            set { DxfHelpers.SetFlag(value, ref Flags, 4); }
        }

        public bool IsLayerChangeAllowed
        {
            get { return DxfHelpers.GetFlag(Flags, 8); }
            set { DxfHelpers.SetFlag(value, ref Flags, 8); }
        }

        public bool IsLinetypeChangeAllowed
        {
            get { return DxfHelpers.GetFlag(Flags, 16); }
            set { DxfHelpers.SetFlag(value, ref Flags, 16); }
        }

        public bool IsLinetypeScaleChangeAllowed
        {
            get { return DxfHelpers.GetFlag(Flags, 32); }
            set { DxfHelpers.SetFlag(value, ref Flags, 32); }
        }

        public bool IsVisibilityChangeAllowed
        {
            get { return DxfHelpers.GetFlag(Flags, 64); }
            set { DxfHelpers.SetFlag(value, ref Flags, 64); }
        }

        public bool IsCloningAllowed
        {
            get { return DxfHelpers.GetFlag(Flags, 128); }
            set { DxfHelpers.SetFlag(value, ref Flags, 128); }
        }

        public bool IsR13FormatProxy
        {
            get { return DxfHelpers.GetFlag(Flags, 32768); }
            set { DxfHelpers.SetFlag(value, ref Flags, 32768); }
        }
    }

    public class DxfClass
    {
        internal const string ClassText = "CLASS";

        public string ClassDxfRecordName { get; set; }
        public string CppClassName { get; set; }
        public string ApplicationName { get; set; }
        public DxfProxyCapabilities ProxyCapabilities { get; set; }
        public int InstanceCount { get; set; }
        public bool WasClassLoadedWithFile { get; set; }
        public bool IsEntity { get; set; }

        internal IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version)
        {
            var list = new List<DxfCodePair>();
            Action<int, object> add = (code, value) => list.Add(new DxfCodePair(code, value));
            add(0, ClassText);
            add(1, ClassDxfRecordName);
            add(2, CppClassName);
            add(3, ApplicationName);
            add(90, ProxyCapabilities.Value);
            if (version >= DxfAcadVersion.R2004)
                add(91, null);
            add(280, (short)(WasClassLoadedWithFile ? 0 : 1));
            add(281, (short)(IsEntity ? 1 : 0));

            return list;
        }

        internal static DxfClass FromBuffer(DxfCodePairBufferReader buffer)
        {
            var cls = new DxfClass();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                buffer.Advance();
                switch (pair.Code)
                {
                    case 1:
                        cls.ClassDxfRecordName = pair.StringValue;
                        break;
                    case 2:
                        cls.CppClassName = pair.StringValue;
                        break;
                    case 3:
                        cls.ApplicationName = pair.StringValue;
                        break;
                    case 90:
                        cls.ProxyCapabilities = new DxfProxyCapabilities(pair.IntegerValue);
                        break;
                    case 91:
                        cls.InstanceCount = pair.IntegerValue;
                        break;
                    case 280:
                        cls.WasClassLoadedWithFile = pair.ShortValue == 0;
                        break;
                    case 281:
                        cls.IsEntity = pair.ShortValue != 0;
                        break;
                }
            }

            return cls;
        }
    }
}
