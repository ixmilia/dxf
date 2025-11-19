using System;

namespace IxMilia.Dxf
{
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
}
