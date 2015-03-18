// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf
{
    public partial class DxfLayer
    {
        public DxfLayer(string name)
            : this()
        {
            Name = name;
        }

        public DxfLayer(string name, DxfColor color)
            : this(name)
        {
            Color = color;
        }
    }

    public partial class DxfViewPort
    {
        public const string ActiveViewPortName = "*ACTIVE";
    }
}
