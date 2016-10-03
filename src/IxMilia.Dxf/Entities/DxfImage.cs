// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfImage
    {
        public IList<DxfPoint> ClippingVertices { get; } = new ListNonNull<DxfPoint>();

        protected override DxfEntity PostParse()
        {
            Debug.Assert((ClippingVertexCount == _clippingVerticesX.Count) && (ClippingVertexCount == _clippingVerticesY.Count));
            foreach (var point in _clippingVerticesX.Zip(_clippingVerticesY, (x, y) => new DxfPoint(x, y, 0.0)))
            {
                ClippingVertices.Add(point);
            }

            _clippingVerticesX.Clear();
            _clippingVerticesY.Clear();
            return this;
        }
    }
}
