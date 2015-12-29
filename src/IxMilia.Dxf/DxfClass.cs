// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

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
        public int ClassVersionNumber { get; set; }
        public DxfProxyCapabilities ProxyCapabilities { get; set; }
        public int InstanceCount { get; set; }
        public bool WasClassLoadedWithFile { get; set; }
        public bool IsEntity { get; set; }

        internal IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version, bool outputHandles)
        {
            var list = new List<DxfCodePair>();
            if (version >= DxfAcadVersion.R14)
            {
                list.Add(new DxfCodePair(0, ClassText));
                list.Add(new DxfCodePair(1, ClassDxfRecordName));
                list.Add(new DxfCodePair(2, CppClassName));
                list.Add(new DxfCodePair(3, ApplicationName));
                list.Add(new DxfCodePair(90, ProxyCapabilities.Value));
                if (version >= DxfAcadVersion.R2004)
                    list.Add(new DxfCodePair(91, InstanceCount));
            }
            else
            {
                // version <= DxfAcadVersion.R13
                list.Add(new DxfCodePair(0, ClassDxfRecordName));
                list.Add(new DxfCodePair(1, CppClassName));
                list.Add(new DxfCodePair(2, ApplicationName));
                list.Add(new DxfCodePair(90, ClassVersionNumber));
            }

            list.Add(new DxfCodePair(280, DxfCommonConverters.BoolShort(!WasClassLoadedWithFile)));
            list.Add(new DxfCodePair(281, DxfCommonConverters.BoolShort(IsEntity)));

            return list;
        }

        internal static DxfClass FromBuffer(DxfCodePairBufferReader buffer, DxfAcadVersion version)
        {
            var cls = new DxfClass();

            // version R13 has varing values for the leading 0 code pair
            var pair = buffer.Peek();
            Debug.Assert(pair.Code == 0);
            if (version <= DxfAcadVersion.R13)
            {
                cls.ClassDxfRecordName = pair.StringValue;
            }
            else
            {
                // swallow (0, CLASS)
                Debug.Assert(pair.StringValue == ClassText);
            }

            buffer.Advance();
            while (buffer.ItemsRemain)
            {
                pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                buffer.Advance();
                switch (pair.Code)
                {
                    case 1:
                        if (version <= DxfAcadVersion.R13)
                            cls.CppClassName = pair.StringValue;
                        else
                            cls.ClassDxfRecordName = pair.StringValue;
                        break;
                    case 2:
                        if (version <= DxfAcadVersion.R13)
                            cls.ApplicationName = pair.StringValue;
                        else
                            cls.CppClassName = pair.StringValue;
                        break;
                    case 3:
                        if (version >= DxfAcadVersion.R14)
                            cls.ApplicationName = pair.StringValue;
                        break;
                    case 90:
                        if (version <= DxfAcadVersion.R13)
                            cls.ClassVersionNumber = pair.IntegerValue;
                        else
                            cls.ProxyCapabilities = new DxfProxyCapabilities(pair.IntegerValue);
                        break;
                    case 91:
                        cls.InstanceCount = pair.IntegerValue;
                        break;
                    case 280:
                        cls.WasClassLoadedWithFile = !DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                    case 281:
                        cls.IsEntity = DxfCommonConverters.BoolShort(pair.ShortValue);
                        break;
                }
            }

            return cls;
        }
    }
}
