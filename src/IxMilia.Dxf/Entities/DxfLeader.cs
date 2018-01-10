// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfLeader
    {
        private ListNonNullWithMinimum<DxfPoint> _vertices = new ListNonNullWithMinimum<DxfPoint>(2);

        public IList<DxfPoint> Vertices { get { return _vertices; } }

        /// <summary>
        /// Creates a new leader entity with the specified vertices.  NOTE, at least 2 vertices must be specified.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="vertices">The vertices to add.</param>
        public DxfLeader(IEnumerable<DxfPoint> vertices)
            : this()
        {
            foreach (var vertex in vertices)
            {
                _vertices.Add(vertex);
            }

            _vertices.ValidateCount();
        }

        protected override DxfEntity PostParse()
        {
            Debug.Assert((VertexCount == _verticesX.Count) && (VertexCount == _verticesY.Count) && (VertexCount == _verticesZ.Count));
            for (int i = 0; i < VertexCount; i++)
            {
                Vertices.Add(new DxfPoint(_verticesX[i], _verticesY[i], _verticesZ[i]));
            }

            _verticesX.Clear();
            _verticesY.Clear();
            _verticesZ.Clear();
            return this;
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            return Vertices;
        }
    }
}
