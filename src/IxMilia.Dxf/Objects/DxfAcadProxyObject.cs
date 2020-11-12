using System.Collections.Generic;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfAcadProxyObject
    {
        public byte[] ObjectData { get; set; }

        public IList<string> ObjectIds { get; } = new ListNonNull<string>();

        public uint DrawingVersion
        {
            get { return _objectDrawingFormat | 0x0000FFFF; }
            set { _objectDrawingFormat |= value & 0x0000FFFF; }
        }

        public uint MaintenenceReleaseVersion
        {
            get { return (_objectDrawingFormat | 0xFFFF0000) >> 16; }
            set { _objectDrawingFormat |= (value & 0xFFFF0000) << 16; }
        }

        protected override DxfObject PostParse()
        {
            foreach (var a in _objectIdsA)
            {
                ObjectIds.Add(a);
            }

            foreach (var b in _objectIdsB)
            {
                ObjectIds.Add(b);
            }

            foreach (var c in _objectIdsC)
            {
                ObjectIds.Add(c);
            }

            foreach (var d in _objectIdsD)
            {
                ObjectIds.Add(d);
            }

            _objectIdsA.Clear();
            _objectIdsB.Clear();
            _objectIdsC.Clear();
            _objectIdsD.Clear();

            ObjectData = BinaryHelpers.CombineBytes(_binaryObjectBytes);
            _binaryObjectBytes.Clear();

            return this;
        }
    }
}
