// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfImage
    {
        private List<DxfPoint> clippingVertices = new List<DxfPoint>();
        public List<DxfPoint> ClippingVertices
        {
            get { return clippingVertices; }
        }

        protected override DxfEntity PostParse()
        {
            Debug.Assert((ClippingVertexCount == _clippingVerticesX.Count) && (ClippingVertexCount == _clippingVerticesY.Count));
            clippingVertices.AddRange(_clippingVerticesX.Zip(_clippingVerticesY, (x, y) => new DxfPoint(x, y, 0.0)));
            _clippingVerticesX.Clear();
            _clippingVerticesY.Clear();
            return this;
        }
    }
}
