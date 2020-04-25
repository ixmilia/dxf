// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Entities
{
    public partial class DxfOleFrame
    {
        public byte[] Data { get; set; }

        protected override DxfEntity PostParse()
        {
            Data = BinaryHelpers.ByteArrayFromStrings(_binaryDataStrings);
            _binaryDataStrings.Clear();
            return this;
        }
    }
}
