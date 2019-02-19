// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfPolyline : IDxfItemInternal
    {
        #region IDxfItemInternal
        IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()
        {
            foreach (var pointer in _vertices.Pointers)
            {
                yield return pointer;
            }

            yield return _seqendPointer;
        }
        #endregion

        /// <summary>
        /// Creates a new polyline entity with the specified vertices.  NOTE, at least 2 vertices must be specified.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="vertices">The vertices to add.</param>
        public DxfPolyline(IEnumerable<DxfVertex> vertices)
            : this(vertices, new DxfSeqend())
        {
        }

        /// <summary>
        /// Creates a new polyline entity with the specified vertices.  NOTE, at least 2 vertices must be specified.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="vertices">The vertices to add.</param>
        /// <param name="seqend">The end sequence entity.</param>
        public DxfPolyline(IEnumerable<DxfVertex> vertices, DxfSeqend seqend)
        {
            Seqend = seqend;
            foreach (var vertex in vertices)
            {
                Vertices.Add(vertex);
            }

            _vertices.ValidateCount();
        }

        public new double Elevation
        {
            get { return Location.Z; }
            set { Location = Location.WithUpdatedZ(value); }
        }

        private DxfPointerList<DxfVertex> _vertices = new DxfPointerList<DxfVertex>(2);
        private DxfPointer _seqendPointer = new DxfPointer(new DxfSeqend());

        public IList<DxfVertex> Vertices { get { return _vertices; } }

        public DxfSeqend Seqend
        {
            get { return (DxfSeqend)_seqendPointer.Item; }
            set { _seqendPointer.Item = value; }
        }

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            base.AddValuePairs(pairs, version, outputHandles);
            var subclassMarker = Is3DPolyline || Is3DPolygonMesh ? "AcDb3dPolyline" : "AcDb2dPolyline";
            pairs.Add(new DxfCodePair(100, subclassMarker));
            if (version <= DxfAcadVersion.R13)
            {
                pairs.Add(new DxfCodePair(66, BoolShort(ContainsVertices)));
            }

            if (version >= DxfAcadVersion.R12)
            {
                pairs.Add(new DxfCodePair(10, Location.X));
                pairs.Add(new DxfCodePair(20, Location.Y));
                pairs.Add(new DxfCodePair(30, Location.Z));
            }

            if (Thickness != 0.0)
            {
                pairs.Add(new DxfCodePair(39, Thickness));
            }

            if (Flags != 0)
            {
                pairs.Add(new DxfCodePair(70, (short)Flags));
            }

            if (DefaultStartingWidth != 0.0)
            {
                pairs.Add(new DxfCodePair(40, DefaultStartingWidth));
            }

            if (DefaultEndingWidth != 0.0)
            {
                pairs.Add(new DxfCodePair(41, DefaultEndingWidth));
            }

            if (PolygonMeshMVertexCount != 0)
            {
                pairs.Add(new DxfCodePair(71, (short)PolygonMeshMVertexCount));
            }

            if (PolygonMeshNVertexCount != 0)
            {
                pairs.Add(new DxfCodePair(72, (short)PolygonMeshNVertexCount));
            }

            if (SmoothSurfaceMDensity != 0)
            {
                pairs.Add(new DxfCodePair(73, (short)SmoothSurfaceMDensity));
            }

            if (SmoothSurfaceNDensity != 0)
            {
                pairs.Add(new DxfCodePair(74, (short)SmoothSurfaceNDensity));
            }

            if (SurfaceType != DxfPolylineCurvedAndSmoothSurfaceType.None)
            {
                pairs.Add(new DxfCodePair(75, (short)SurfaceType));
            }

            if (Normal != DxfVector.ZAxis)
            {
                pairs.Add(new DxfCodePair(210, Normal.X));
                pairs.Add(new DxfCodePair(220, Normal.Y));
                pairs.Add(new DxfCodePair(230, Normal.Z));
            }

            if (XData != null)
            {
                XData.AddValuePairs(pairs, version, outputHandles);
            }
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var vertex in Vertices)
            {
                vertex.Is3DPolylineVertex = this.Is3DPolyline;
                vertex.Is3DPolygonMesh = this.Is3DPolygonMesh;
                pairs.AddRange(vertex.GetValuePairs(version, outputHandles));
            }

            if (Seqend != null)
            {
                pairs.AddRange(Seqend.GetValuePairs(version, outputHandles));
            }
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            yield return Location;
            int n = Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                var vertex = Vertices[i];
                if (Math.Abs(vertex.Bulge) <= 1e-10)
                {
                    yield return vertex.Location;
                }
                else
                {
                    // the segment between `vertex.Location` and `next.Location` is an arc
                    var next = Vertices[(i + 1) % n];
                    DxfBoundingBox bbox;
                    if (TryGetArcBoundingBox(vertex, next, out bbox))
                    {
                        yield return bbox.MinimumPoint;
                        yield return bbox.MaximumPoint;
                    }
                    else
                    {
                        // fallback if points are too close / bulge is tiny
                        yield return vertex.Location;
                    }
                }
            }
        }

        private static bool TryGetArcBoundingBox(DxfVertex v1, DxfVertex v2, out DxfBoundingBox bbox)
        {
            DxfArc arc;
            if (!DxfArc.TryCreateFromVertices(v1, v2, out arc))
            {
                bbox = default(DxfBoundingBox);
                return false;
            }
            DxfBoundingBox? boundingBox = arc.GetBoundingBox();
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
