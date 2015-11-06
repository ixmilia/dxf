// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace IxMilia.Dxf.Objects
{
    public partial class DxfImageDefinitionReactor
    {
        public uint AssociatedImage
        {
            get { return OwnerHandle; }
            set { OwnerHandle = value; }
        }
    }
}
