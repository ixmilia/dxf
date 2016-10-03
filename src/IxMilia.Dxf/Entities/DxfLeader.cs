// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfLeader
    {
        public IList<DxfPoint> Vertices { get; } = new ListNonNull<DxfPoint>();

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
    }
}
