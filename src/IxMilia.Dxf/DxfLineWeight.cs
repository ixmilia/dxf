// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf
{
    public enum DxfLineWeightType : short
    {
        Standard = -3,
        ByLayer = -2,
        ByBlock = -1,
        Custom = 0
    }

    public class DxfLineWeight
    {
        public short Value { get; set; }

        public DxfLineWeightType LineWeightType
        {
            get
            {
                switch (Value)
                {
                    case -3:
                        return DxfLineWeightType.Standard;
                    case -2:
                        return DxfLineWeightType.ByLayer;
                    case -1:
                        return DxfLineWeightType.ByBlock;
                    default:
                        return DxfLineWeightType.Custom;
                }
            }
        }

        public void SetStandard()
        {
            Value = (short)DxfLineWeightType.Standard;
        }

        public void SetByLayer()
        {
            Value = (short)DxfLineWeightType.ByLayer;
        }

        public void SetByBlock()
        {
            Value = (short)DxfLineWeightType.ByBlock;
        }

        public DxfLineWeight()
            : this((short)DxfLineWeightType.Standard)
        {
        }

        private DxfLineWeight(short value)
        {
            Value = value;
        }

        internal static DxfLineWeight FromRawValue(short value)
        {
            return new DxfLineWeight(value);
        }

        internal static short GetRawValue(DxfLineWeight lineWeight)
        {
            return lineWeight.Value;
        }
    }
}
