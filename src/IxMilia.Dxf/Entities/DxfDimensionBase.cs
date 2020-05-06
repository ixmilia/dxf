// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Entities
{
    public partial class DxfDimensionBase : DxfEntity
    {
        public bool IsBlockReferenceReferencedByThisBlockOnly { get; set; } = false;

        public bool IsOrdinateXType { get; set; } = false;

        public bool IsAtUserDefinedLocation { get; set; } = false;

        protected override void CopyManualValues(DxfEntity other)
        {
            if (other is DxfDimensionBase otherDim)
            {
                IsBlockReferenceReferencedByThisBlockOnly = otherDim.IsBlockReferenceReferencedByThisBlockOnly;
                IsOrdinateXType = otherDim.IsOrdinateXType;
                IsAtUserDefinedLocation = otherDim.IsAtUserDefinedLocation;
            }
        }

        private DxfDimensionType HandleDimensionType(short value)
        {
            // reader
            IsBlockReferenceReferencedByThisBlockOnly = (value & 32) == 32;
            IsOrdinateXType = (value & 64) == 64;
            IsAtUserDefinedLocation = (value & 128) == 128;
            return (DxfDimensionType)(value & 0x0F); // only the lower 4 bits matter
        }

        private short HandleDimensionType(DxfDimensionType dimensionType)
        {
            // writer
            var value = (short)dimensionType;
            if (IsBlockReferenceReferencedByThisBlockOnly)
            {
                value |= 32;
            }

            if (IsOrdinateXType)
            {
                value |= 64;
            }

            if (IsAtUserDefinedLocation)
            {
                value |= 128;
            }

            return value;
        }
    }
}
