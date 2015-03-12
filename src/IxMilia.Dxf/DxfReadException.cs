// This line is required for T4 template generation to work. <#+

#if DXF
using System;

namespace IxMilia.Dxf
{
#endif
    public class DxfReadException : Exception
    {
        public DxfReadException()
            : base()
        {
        }

        public DxfReadException(string message)
            : base(message)
        {
        }

        public DxfReadException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }

#if DXF
}
#endif

// This line is required for T4 template generation to work. #>
