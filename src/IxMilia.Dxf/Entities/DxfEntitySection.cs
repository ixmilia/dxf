// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfEntitySection
    {
        public IList<DxfPoint> Vertices { get; } = new List<DxfPoint>();
        public IList<DxfPoint> BackLineVertices { get; } = new List<DxfPoint>();

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

            Debug.Assert(_backLineVertexCount == _backLineVertexX.Count && _backLineVertexCount == _backLineVertexY.Count && _backLineVertexCount == _backLineVertexZ.Count);
            for (int i = 0; i < _backLineVertexCount; i++)
            {
                BackLineVertices.Add(new DxfPoint(_backLineVertexX[i], _backLineVertexY[i], _backLineVertexZ[i]));
            }

            _backLineVertexX.Clear();
            _backLineVertexY.Clear();
            _backLineVertexZ.Clear();

            return this;
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            return Vertices.Concat(BackLineVertices);
        }
    }
}
