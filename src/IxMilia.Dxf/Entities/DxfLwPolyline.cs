using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfLwPolyline
    {
        private ListNonNullWithMinimum<DxfLwPolylineVertex> _vertices = new ListNonNullWithMinimum<DxfLwPolylineVertex>(2);

        public IList<DxfLwPolylineVertex> Vertices { get { return _vertices; } }

        /// <summary>
        /// Creates a new LW polyline entity with the specified vertices.  NOTE, at least 2 vertices must be specified.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="vertices">The vertices to add.</param>
        public DxfLwPolyline(IEnumerable<DxfLwPolylineVertex> vertices)
            : this()
        {
            foreach (var vertex in vertices)
            {
                _vertices.Add(vertex);
            }

            _vertices.ValidateCount();
        }

        internal override DxfEntity PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                while (this.TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
                }

                if (pair.Code == 0)
                {
                    break;
                }

                switch (pair.Code)
                {
                    // vertex-specific pairs
                    case 10:
                        // start a new vertex
                        Vertices.Add(new DxfLwPolylineVertex());
                        Vertices.Last().X = pair.DoubleValue;
                        break;
                    case 20:
                        Vertices.Last().Y = pair.DoubleValue;
                        break;
                    case 40:
                        Vertices.Last().StartingWidth = pair.DoubleValue;
                        break;
                    case 41:
                        Vertices.Last().EndingWidth = pair.DoubleValue;
                        break;
                    case 42:
                        Vertices.Last().Bulge = pair.DoubleValue;
                        break;
                    case 91:
                        Vertices.Last().Identifier = pair.IntegerValue;
                        break;
                    // all other pairs
                    case 39:
                        Thickness = pair.DoubleValue;
                        break;
                    case 43:
                        ConstantWidth = pair.DoubleValue;
                        break;
                    case 70:
                        Flags = pair.ShortValue;
                        break;
                    case 210:
                        ExtrusionDirection = ExtrusionDirection.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 220:
                        ExtrusionDirection = ExtrusionDirection.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 230:
                        ExtrusionDirection = ExtrusionDirection.WithUpdatedZ(pair.DoubleValue);
                        break;
                    default:
                        if (!base.TrySetPair(pair))
                        {
                            ExcessCodePairs.Add(pair);
                        }
                        break;
                }

                buffer.Advance();
            }

            return PostParse();
        }
        
        private DxfEntity VertexPairToEntity(DxfLwPolylineVertex vertex1, DxfLwPolylineVertex vertex2)
        {
            if (Math.Abs(vertex1.Bulge) <= 1e-10)
            {
                return new DxfLine(new DxfPoint(vertex1.X, vertex1.Y, 0), new DxfPoint(vertex2.X, vertex2.Y, 0));
            }

            // the segment between `vertex.Location` and `next.Location` is an arc
            if (DxfArc.TryCreateFromVertices(vertex1, vertex2, out var arc))
            {
                return arc;
            }
            else 
            {
                // fallback if points are too close / bulge is tiny
                return new DxfLine(new DxfPoint(vertex1.X, vertex1.Y, 0), new DxfPoint(vertex2.X, vertex2.Y, 0));
            }
        }

        /// <summary>
        /// Converts DxfLwPolyline into a collection of DxfArc and DxfLine entities
        /// </summary>
        public IEnumerable<DxfEntity> AsSimpleEntities()
        {
            int n = Vertices.Count;

            for (var i = 0; i < n - 1; i++)
            {
                var result = VertexPairToEntity(Vertices[i], Vertices[i + 1]);
                result.CopyCommonPropertiesFrom(this);
                yield return result;
            }

            if (IsClosed)
            {
                var result = VertexPairToEntity(Vertices[n - 1], Vertices[0]);
                result.CopyCommonPropertiesFrom(this);
                yield return result;
            }
        } 
        
        private IEnumerable<DxfPoint> VertexPairToBoundingPoints(DxfLwPolylineVertex vertex1, DxfLwPolylineVertex vertex2)
        {
            if (Math.Abs(vertex1.Bulge) <= 1e-10)
            {
                yield return new DxfPoint(vertex1.X, vertex1.Y, 0);
            }
            else
            {
                // the segment between `vertex.Location` and `next.Location` is an arc
                if (TryGetArcBoundingBox(vertex1, vertex2, out var bbox))
                {
                    yield return bbox.MinimumPoint;
                    yield return bbox.MaximumPoint;
                }
                else
                {
                    // fallback if points are too close / bulge is tiny
                    yield return new DxfPoint(vertex1.X, vertex1.Y, 0);
                }
            }
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            int n = Vertices.Count;

            for (var i = 0; i < n - 1; i++)
            {
                foreach (var point in VertexPairToBoundingPoints(Vertices[i], Vertices[i + 1])) yield return point;
            }

            if (IsClosed)
            {
                foreach (var point in VertexPairToBoundingPoints(Vertices[n - 1], Vertices[0])) yield return point;
            }
        }

        private static bool TryGetArcBoundingBox(DxfLwPolylineVertex v1, DxfLwPolylineVertex v2, out DxfBoundingBox bbox)
        {
            if (!DxfArc.TryCreateFromVertices(v1, v2, out var arc))
            {
                bbox = default(DxfBoundingBox);
                return false;
            }

            var boundingBox = arc.GetBoundingBox();
            if (!boundingBox.HasValue)
            {
                bbox = default(DxfBoundingBox);
                return false;
            }

            bbox = boundingBox.Value;
            return true;
        }
    }
}