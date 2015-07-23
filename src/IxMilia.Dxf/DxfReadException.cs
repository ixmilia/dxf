// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This line is required for T4 template generation to work. <#+

#if DXF
using System;

namespace IxMilia.Dxf
{
#endif
    public class DxfReadException : Exception
    {
        public int Offset { get; private set; }

        public DxfReadException(string message, int offset)
            : base(message)
        {
            Offset = offset;
        }

        public DxfReadException(string message, DxfCodePair pair)
            : base(message)
        {
            Offset = pair == null ? -1 : pair.Offset;
        }
    }

#if DXF
}
#endif

// This line is required for T4 template generation to work. #>
