// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Entities
{
    public partial class DxfAngularTwoLineDimension
    {
        public DxfPoint SecondExtensionLineP1
        {
            get => DefinitionPoint1;
            set => DefinitionPoint1 = value;
        }
    }
}
