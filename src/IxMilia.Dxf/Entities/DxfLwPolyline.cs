// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfLwPolyline
    {
        public class DxfLwPolylineVertex
        {
            public DxfPoint Location { get; set; }
            public int Identifier { get; set; }
            public double StartingWidth { get; set; }
            public double EndingWidth { get; set; }
            public double Bulge { get; set; }
        }

        private List<DxfLwPolylineVertex> vertices = new List<DxfLwPolylineVertex>();
        public List<DxfLwPolylineVertex> Vertices
        {
            get { return vertices; }
        }

        protected override DxfEntity PostParse()
        {
            Debug.Assert((VertexCount == _vertexCoordinateX.Count) && (VertexCount == _vertexCoordinateY.Count));
            // TODO: how to read optional starting/ending width and bulge in this way?
            vertices.AddRange(_vertexCoordinateX.Zip(_vertexCoordinateY, (x, y) => new DxfLwPolylineVertex() { Location = new DxfPoint(x, y, 0.0) }));
            return this;
        }
    }
}
