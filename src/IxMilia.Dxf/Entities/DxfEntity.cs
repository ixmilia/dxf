// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Entities
{
    public enum DxfHorizontalTextJustification
    {
        Left = 0,
        Center = 1,
        Right = 2,
        Aligned = 3,
        Middle = 4,
        Fit = 5
    }

    public enum DxfVerticalTextJustification
    {
        Baseline = 0,
        Bottom = 1,
        Middle = 2,
        Top = 3
    }

    public enum DxfPolylineCurvedAndSmoothSurfaceType
    {
        None = 0,
        QuadraticBSpline = 5,
        CubicBSpline = 6,
        Bezier = 8
    }

    public enum DxfImageClippingBoundaryType
    {
        Rectangular = 1,
        Polygonal = 2
    }

    public enum DxfLeaderPathType
    {
        StraightLineSegments = 0,
        Spline = 1
    }

    public enum DxfLeaderCreationAnnotationType
    {
        WithTextAnnotation = 0,
        WithToleranceAnnotation = 1,
        WithBlockReferenceAnnotation = 2,
        NoAnnotation = 3
    }

    public enum DxfLeaderHooklineDirection
    {
        OppositeFromHorizontalVector = 0,
        SameAsHorizontalVector = 1
    }

    public enum DxfOleObjectType
    {
        Link = 1,
        Embedded = 2,
        Static = 3
    }

    public enum DxfTileModeDescriptor
    {
        InTiledViewport = 0,
        InNonTiledViewport = 1
    }

    public enum DxfFontType
    {
        TTF = 0,
        SHX = 1
    }

    public enum DxfDimensionType
    {
        RotatedHorizontalOrVertical = 0,
        Aligned = 1,
        Angular = 2,
        Diameter = 3,
        Radius = 4,
        AngularThreePoint = 5,
        Ordinate = 6
    }

    public enum DxfAttachmentPoint
    {
        TopLeft = 1,
        TopCenter = 2,
        TopRight = 3,
        MiddleLeft = 4,
        MiddleCenter = 5,
        MiddleRight = 6,
        BottomLeft = 7,
        BottomCenter = 8,
        BottomRight = 9
    }

    public enum DxfTextLineSpacingStyle
    {
        AtLeast = 1,
        Exact = 2
    }

    public enum DxfDimensionVersion
    {
        R2010 = 0
    }

    public abstract partial class DxfEntity
    {
        protected List<DxfCodePair> ExcessCodePairs = new List<DxfCodePair>();

        public abstract DxfEntityType EntityType { get; }

        protected virtual DxfAcadVersion MinVersion
        {
            get { return DxfAcadVersion.Min; }
        }

        protected virtual DxfAcadVersion MaxVersion
        {
            get { return DxfAcadVersion.Max; }
        }

        protected virtual void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version)
        {
        }

        protected virtual DxfEntity PostParse()
        {
            return this;
        }

        public IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version)
        {
            var pairs = new List<DxfCodePair>();
            if (version >= MinVersion && version <= MaxVersion)
            {
                AddValuePairs(pairs, version);
                AddTrailingCodePairs(pairs, version);
            }

            return pairs;
        }

        internal virtual DxfEntity PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                if (!TrySetPair(pair))
                {
                    ExcessCodePairs.Add(pair);
                }

                buffer.Advance();
            }

            return PostParse();
        }

        protected static bool BoolShort(short s)
        {
            return s != 0;
        }

        protected static short BoolShort(bool b)
        {
            return (short)(b ? 1 : 0);
        }

        private static short NotBoolShort(bool b)
        {
            return BoolShort(!b);
        }

        private static void SwallowEntity(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                    break;
                buffer.Advance();
            }
        }
    }

    public partial class DxfProxyEntity
    {
        public int ObjectDrawingFormatVersion
        {
            // lower word
            get { return (int)(ObjectDrawingFormat & 0xFFFF); }
            set { ObjectDrawingFormat |= (uint)value & 0xFFFF; }
        }

        public int ObjectMaintenanceReleaseVersion
        {
            // upper word
            get { return (int)(ObjectDrawingFormat >> 4); }
            set { ObjectDrawingFormat = (uint)(value << 4) + ObjectDrawingFormat & 0xFFFF; }
        }
    }

    public partial class DxfPolyline
    {
        public double Elevation
        {
            get { return Location.Z; }
            set { Location.Z = value; }
        }

        private List<DxfVertex> vertices = new List<DxfVertex>();
        private DxfSeqend seqend = new DxfSeqend();

        public List<DxfVertex> Vertices { get { return vertices; } }

        public DxfSeqend Seqend
        {
            get { return seqend; }
            set { seqend = value; }
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version)
        {
            foreach (var vertex in Vertices)
            {
                pairs.AddRange(vertex.GetValuePairs(version));
            }

            if (Seqend != null)
            {
                pairs.AddRange(Seqend.GetValuePairs(version));
            }
        }
    }

    public partial class DxfLeader
    {
        private List<DxfPoint> vertices = new List<DxfPoint>();
        public List<DxfPoint> Vertices
        {
            get { return vertices; }
        }

        protected override DxfEntity PostParse()
        {
            Debug.Assert((VertexCount == VerticesX.Count) && (VertexCount == VerticesY.Count) && (VertexCount == VerticesZ.Count));
            for (int i = 0; i < VertexCount; i++)
            {
                Vertices.Add(new DxfPoint(VerticesX[i], VerticesY[i], VerticesZ[i]));
            }

            VerticesX.Clear();
            VerticesY.Clear();
            VerticesZ.Clear();
            return this;
        }
    }

    public partial class DxfImage
    {
        private List<DxfPoint> clippingVertices = new List<DxfPoint>();
        public List<DxfPoint> ClippingVertices
        {
            get { return clippingVertices; }
        }

        protected override DxfEntity PostParse()
        {
            Debug.Assert((ClippingVertexCount == ClippingVerticesX.Count) && (ClippingVertexCount == ClippingVerticesY.Count));
            clippingVertices.AddRange(ClippingVerticesX.Zip(ClippingVerticesY, (x, y) => new DxfPoint(x, y, 0.0)));
            ClippingVerticesX.Clear();
            ClippingVerticesY.Clear();
            return this;
        }
    }

    public partial class DxfInsert
    {
        private List<DxfAttribute> attributes = new List<DxfAttribute>();
        private DxfSeqend seqend = new DxfSeqend();

        public List<DxfAttribute> Attributes { get { return attributes; } }

        public DxfSeqend Seqend
        {
            get { return seqend; }
            set { seqend = value; }
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version)
        {
            foreach (var attribute in Attributes)
            {
                pairs.AddRange(attribute.GetValuePairs(version));
            }

            if (Seqend != null)
            {
                pairs.AddRange(Seqend.GetValuePairs(version));
            }
        }
    }

    public partial class DxfLwPolyline
    {
        public class DxfLwPolylineVertex
        {
            public DxfPoint Location { get; set; }
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
            Debug.Assert((VertexCount == VertexCoordinateX.Count) && (VertexCount == VertexCoordinateY.Count));
            // TODO: how to read optional starting/ending width and bulge in this way?
            vertices.AddRange(VertexCoordinateX.Zip(VertexCoordinateY, (x, y) => new DxfLwPolylineVertex() { Location = new DxfPoint(x, y, 0.0) }));
            return this;
        }
    }

    public partial class DxfSpline
    {
        public int NumberOfKnots
        {
            get { return KnotValues.Count; }
        }

        public int NumberOfControlPoints
        {
            get { return ControlPoints.Count; }
        }

        public int NumberOfFitPoints
        {
            get { return FitPoints.Count; }
        }

        private List<DxfPoint> controlPoints = new List<DxfPoint>();
        public List<DxfPoint> ControlPoints
        {
            get { return controlPoints; }
        }

        private List<DxfPoint> fitPoints = new List<DxfPoint>();
        public List<DxfPoint> FitPoints
        {
            get { return fitPoints; }
        }

        protected override DxfEntity PostParse()
        {
            Debug.Assert((ControlPointX.Count == ControlPointY.Count) && (ControlPointX.Count == ControlPointZ.Count));
            for (int i = 0; i < ControlPointX.Count; i++)
            {
                controlPoints.Add(new DxfPoint(ControlPointX[i], ControlPointY[i], ControlPointZ[i]));
            }

            ControlPointX.Clear();
            ControlPointY.Clear();
            ControlPointZ.Clear();

            Debug.Assert((FitPointX.Count == FitPointY.Count) && (FitPointX.Count == FitPointZ.Count));
            for (int i = 0; i < FitPointX.Count; i++)
            {
                fitPoints.Add(new DxfPoint(FitPointX[i], FitPointY[i], FitPointZ[i]));
            }

            FitPointX.Clear();
            FitPointY.Clear();
            FitPointZ.Clear();

            return this;
        }
    }
}
