// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfMLine
    {
        public List<DxfPoint> Vertices { get; } = new List<DxfPoint>();
        public List<DxfVector> SegmentDirections { get; } = new List<DxfVector>();
        public List<DxfVector> MiterDirections { get; } = new List<DxfVector>();

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
    }
}
