using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfMLine
    {
        public IList<DxfPoint> Vertices { get; } = new List<DxfPoint>();
        public IList<DxfVector> SegmentDirections { get; } = new List<DxfVector>();
        public IList<DxfVector> MiterDirections { get; } = new List<DxfVector>();

        protected override DxfEntity PostParse()
        {
            Debug.Assert(_vertexCount == _vertexX.Count && _vertexCount == _vertexY.Count && _vertexCount == _vertexZ.Count);
            for (int i = 0; i < _vertexCount; i++)
            {
                Vertices.Add(new DxfPoint(_vertexX[i], _vertexY[i], _vertexZ[i]));
            }

            _vertexX.Clear();
            _vertexY.Clear();
            _vertexZ.Clear();

            Debug.Assert(_vertexCount == _segmentDirectionX.Count && _vertexCount == _segmentDirectionY.Count && _vertexCount == _segmentDirectionZ.Count);
            for (int i = 0; i < _vertexCount; i++)
            {
                Vertices.Add(new DxfPoint(_segmentDirectionX[i], _segmentDirectionY[i], _segmentDirectionZ[i]));
            }

            _segmentDirectionX.Clear();
            _segmentDirectionY.Clear();
            _segmentDirectionZ.Clear();

            Debug.Assert(_vertexCount == _miterDirectionX.Count && _vertexCount == _miterDirectionY.Count && _vertexCount == _miterDirectionZ.Count);
            for (int i = 0; i < _vertexCount; i++)
            {
                Vertices.Add(new DxfPoint(_miterDirectionX[i], _miterDirectionY[i], _miterDirectionZ[i]));
            }

            _miterDirectionX.Clear();
            _miterDirectionY.Clear();
            _miterDirectionZ.Clear();

            return this;
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            return Vertices;
        }
    }
}
