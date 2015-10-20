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

    public enum DxfVersion
    {
        R2010 = 0
    }

    public enum DxfHelixConstraint
    {
        ConstrainTurnHeight = 0,
        ConstrainTurns = 1,
        ConstrainHeight = 2
    }

    public enum DxfLightType
    {
        Distant = 1,
        Point = 2,
        Spot = 3
    }

    public enum DxfLightAttenuationType
    {
        None = 0,
        InverseLinear = 1,
        InverseSquare = 2
    }

    public enum DxfTextAttachmentDirection
    {
        Horizontal = 0,
        Vertical = 1
    }

    public enum DxfBottomTextAttachmentDirection
    {
        Center = 9,
        UnderlineAndCenter = 10
    }

    public enum DxfTopTextAttachmentDirection
    {
        Center = 9,
        OverlineAndCenter = 10
    }

    public enum DxfDrawingDirection
    {
        LeftToRight = 1,
        TopToBottom = 3,
        ByStyle = 5
    }

    public enum DxfMTextLineSpacingStyle
    {
        AtLeast = 1,
        Exact = 2
    }

    public enum DxfBackgroundFillSetting
    {
        Off = 0,
        UseBackgroundFillColor = 1,
        UseDrawingWindowColor = 2
    }

    public enum DxfMTextFlag
    {
        MultilineAttribute = 2,
        ConstantMultilineAttributeDefinition = 4
    }

    public abstract partial class DxfEntity
    {
        protected List<DxfCodePair> ExcessCodePairs = new List<DxfCodePair>();
        protected DxfXData XDataProtected { get; set; }
        public List<DxfCodePairGroup> ExtensionDataGroups { get; private set; }

        public abstract DxfEntityType EntityType { get; }

        protected virtual DxfAcadVersion MinVersion
        {
            get { return DxfAcadVersion.Min; }
        }

        protected virtual DxfAcadVersion MaxVersion
        {
            get { return DxfAcadVersion.Max; }
        }

        protected DxfEntity()
        {
            Initialize();
            ExtensionDataGroups = new List<DxfCodePairGroup>();
        }

        protected virtual void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
        }

        protected virtual DxfEntity PostParse()
        {
            return this;
        }

        public IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version, bool outputHandles)
        {
            var pairs = new List<DxfCodePair>();
            if (version >= MinVersion && version <= MaxVersion)
            {
                AddValuePairs(pairs, version, outputHandles);
                AddTrailingCodePairs(pairs, version, outputHandles);
            }

            return pairs;
        }

        private void AddExtensionValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var group in ExtensionDataGroups)
            {
                group.AddValuePairs(pairs, version, outputHandles);
            }
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
                else if (pair.Code == DxfCodePairGroup.GroupCodeNumber)
                {
                    buffer.Advance();
                    var groupName = DxfCodePairGroup.GetGroupName(pair.StringValue);
                    ExtensionDataGroups.Add(DxfCodePairGroup.FromBuffer(buffer, groupName));
                }
                else if (pair.Code == (int)DxfXDataType.ApplicationName)
                {
                    XDataProtected = DxfXData.FromBuffer(buffer, pair.StringValue);
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
            return DxfCommonConverters.BoolShort(s);
        }

        protected static short BoolShort(bool b)
        {
            return DxfCommonConverters.BoolShort(b);
        }

        private static short NotBoolShort(bool b)
        {
            return BoolShort(!b);
        }

        protected static uint UIntHandle(string s)
        {
            return DxfCommonConverters.UIntHandle(s);
        }

        protected static string UIntHandle(uint u)
        {
            return DxfCommonConverters.UIntHandle(u);
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

    public partial class DxfAttribute : IDxfHasChildrenWithHandle
    {
        private const string AcDbXrecordText = "AcDbXrecord";
        private string _lastSubclassMarker;
        private bool _isVersionSet;
        private int _xrecCode70Count = 0;

        public DxfMText MText { get; internal set; } = new DxfMText();

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 100:
                    _lastSubclassMarker = pair.StringValue;
                    break;
                case 1:
                    this.Value = (pair.StringValue);
                    break;
                case 2:
                    if (_lastSubclassMarker == AcDbXrecordText) XRecordTag = pair.StringValue;
                    else AttributeTag = pair.StringValue;
                    break;
                case 7:
                    this.TextStyleName = (pair.StringValue);
                    break;
                case 10:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.X = pair.DoubleValue;
                    else Location.X = pair.DoubleValue;
                    break;
                case 20:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.Y = pair.DoubleValue;
                    else Location.Y = pair.DoubleValue;
                    break;
                case 30:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.Z = pair.DoubleValue;
                    else Location.Z = pair.DoubleValue;
                    break;
                case 11:
                    this.SecondAlignmentPoint.X = pair.DoubleValue;
                    break;
                case 21:
                    this.SecondAlignmentPoint.Y = pair.DoubleValue;
                    break;
                case 31:
                    this.SecondAlignmentPoint.Z = pair.DoubleValue;
                    break;
                case 39:
                    this.Thickness = (pair.DoubleValue);
                    break;
                case 40:
                    if (_lastSubclassMarker == AcDbXrecordText) AnnotationScale = pair.DoubleValue;
                    else TextHeight = pair.DoubleValue;
                    break;
                case 41:
                    this.RelativeXScaleFactor = (pair.DoubleValue);
                    break;
                case 50:
                    this.Rotation = (pair.DoubleValue);
                    break;
                case 51:
                    this.ObliqueAngle = (pair.DoubleValue);
                    break;
                case 70:
                    if (_lastSubclassMarker == AcDbXrecordText)
                    {
                        switch (_xrecCode70Count)
                        {
                            case 0:
                                MTextFlag = (DxfMTextFlag)pair.ShortValue;
                                break;
                            case 1:
                                IsReallyLocked = BoolShort(pair.ShortValue);
                                break;
                            case 2:
                                SecondaryAttributeCount = pair.ShortValue;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values");
                                break;
                        }

                        _xrecCode70Count++;
                    }
                    else
                    {
                        Flags = pair.ShortValue;
                    }
                    break;
                case 71:
                    this.TextGenerationFlags = (int)(pair.ShortValue);
                    break;
                case 72:
                    this.HorizontalTextJustification = (DxfHorizontalTextJustification)(pair.ShortValue);
                    break;
                case 73:
                    this.FieldLength = (pair.ShortValue);
                    break;
                case 74:
                    this.VerticalTextJustification = (DxfVerticalTextJustification)(pair.ShortValue);
                    break;
                case 210:
                    this.Normal.X = pair.DoubleValue;
                    break;
                case 220:
                    this.Normal.Y = pair.DoubleValue;
                    break;
                case 230:
                    this.Normal.Z = pair.DoubleValue;
                    break;
                case 280:
                    if (_lastSubclassMarker == AcDbXrecordText) KeepDuplicateRecords = pair.BoolValue;
                    else if (!_isVersionSet)
                    {
                        Version = (DxfVersion)pair.ShortValue;
                        _isVersionSet = true;
                    }
                    else IsLockedInBlock = BoolShort(pair.ShortValue);
                    break;
                case 340:
                    this.SecondaryAttributeHandles.Add(UIntHandle(pair.StringValue));
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            pairs.AddRange(MText.GetValuePairs(version, outputHandles));
        }

        IEnumerable<IDxfHasHandle> IDxfHasChildrenWithHandle.GetChildren()
        {
            if (MText != null)
            {
                yield return MText;
            }
        }
    }

    public partial class DxfAttributeDefinition : IDxfHasChildrenWithHandle
    {
        private const string AcDbXrecordText = "AcDbXrecord";
        private string _lastSubclassMarker;
        private bool _isVersionSet;
        private int _xrecCode70Count = 0;

        public DxfMText MText { get; internal set; } = new DxfMText();

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 100:
                    _lastSubclassMarker = pair.StringValue;
                    break;
                case 1:
                    Value = pair.StringValue;
                    break;
                case 2:
                    if (_lastSubclassMarker == AcDbXrecordText) XRecordTag = pair.StringValue;
                    else TextTag = pair.StringValue;
                    break;
                case 3:
                    Prompt = pair.StringValue;
                    break;
                case 7:
                    TextStyleName = pair.StringValue;
                    break;
                case 10:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.X = pair.DoubleValue;
                    else Location.X = pair.DoubleValue;
                    break;
                case 20:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.Y = pair.DoubleValue;
                    else Location.Y = pair.DoubleValue;
                    break;
                case 30:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.Z = pair.DoubleValue;
                    else Location.Z = pair.DoubleValue;
                    break;
                case 11:
                    SecondAlignmentPoint.X = pair.DoubleValue;
                    break;
                case 21:
                    SecondAlignmentPoint.Y = pair.DoubleValue;
                    break;
                case 31:
                    SecondAlignmentPoint.Z = pair.DoubleValue;
                    break;
                case 39:
                    Thickness = pair.DoubleValue;
                    break;
                case 40:
                    if (_lastSubclassMarker == AcDbXrecordText) AnnotationScale = pair.DoubleValue;
                    else TextHeight = pair.DoubleValue;
                    break;
                case 41:
                    RelativeXScaleFactor = pair.DoubleValue;
                    break;
                case 50:
                    Rotation = pair.DoubleValue;
                    break;
                case 51:
                    ObliqueAngle = pair.DoubleValue;
                    break;
                case 70:
                    if (_lastSubclassMarker == AcDbXrecordText)
                    {
                        switch (_xrecCode70Count)
                        {
                            case 0:
                                MTextFlag = (DxfMTextFlag)pair.ShortValue;
                                break;
                            case 1:
                                IsReallyLocked = BoolShort(pair.ShortValue);
                                break;
                            case 2:
                                SecondaryAttributeCount = pair.ShortValue;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values");
                                break;
                        }

                        _xrecCode70Count++;
                    }
                    else
                    {
                        Flags = pair.ShortValue;
                    }
                    break;
                case 71:
                    TextGenerationFlags = pair.ShortValue;
                    break;
                case 72:
                    HorizontalTextJustification = (DxfHorizontalTextJustification)pair.ShortValue;
                    break;
                case 73:
                    FieldLength = pair.ShortValue;
                    break;
                case 74:
                    VerticalTextJustification = (DxfVerticalTextJustification)pair.ShortValue;
                    break;
                case 210:
                    Normal.X = pair.DoubleValue;
                    break;
                case 220:
                    Normal.Y = pair.DoubleValue;
                    break;
                case 230:
                    Normal.Z = pair.DoubleValue;
                    break;
                case 280:
                    if (_lastSubclassMarker == AcDbXrecordText) KeepDuplicateRecords = pair.BoolValue;
                    else if (!_isVersionSet)
                    {
                        Version = (DxfVersion)pair.ShortValue;
                        _isVersionSet = true;
                    }
                    else IsLockedInBlock = BoolShort(pair.ShortValue);
                    break;
                case 340:
                    SecondaryAttributeHandles.Add(UIntHandle(pair.StringValue));
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            pairs.AddRange(MText.GetValuePairs(version, outputHandles));
        }

        IEnumerable<IDxfHasHandle> IDxfHasChildrenWithHandle.GetChildren()
        {
            if (MText != null)
            {
                yield return MText;
            }
        }
    }

    public partial class DxfMText
    {
        private bool _readingColumnData = false;
        private bool _readColumnCount = false;

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 1:
                    this.Text = (pair.StringValue);
                    break;
                case 3:
                    ExtendedText.Add(pair.StringValue);
                    break;
                case 7:
                    this.TextStyleName = (pair.StringValue);
                    break;
                case 10:
                    this.InsertionPoint.X = pair.DoubleValue;
                    break;
                case 20:
                    this.InsertionPoint.Y = pair.DoubleValue;
                    break;
                case 30:
                    this.InsertionPoint.Z = pair.DoubleValue;
                    break;
                case 11:
                    this.XAxisDirection.X = pair.DoubleValue;
                    break;
                case 21:
                    this.XAxisDirection.Y = pair.DoubleValue;
                    break;
                case 31:
                    this.XAxisDirection.Z = pair.DoubleValue;
                    break;
                case 40:
                    this.InitialTextHeight = (pair.DoubleValue);
                    break;
                case 41:
                    this.ReferenceRectangleWidth = (pair.DoubleValue);
                    break;
                case 42:
                    this.HorizontalWidth = (pair.DoubleValue);
                    break;
                case 43:
                    this.VerticalHeight = (pair.DoubleValue);
                    break;
                case 44:
                    this.LineSpacingFactor = (pair.DoubleValue);
                    break;
                case 45:
                    this.FillBoxScale = (pair.DoubleValue);
                    break;
                case 48:
                    this.ColumnWidth = (pair.DoubleValue);
                    break;
                case 49:
                    this.ColumnGutter = (pair.DoubleValue);
                    break;
                case 50:
                    if (_readingColumnData)
                    {
                        if (_readColumnCount)
                        {
                            ColumnHeights.Add(pair.DoubleValue);
                        }
                        else
                        {
                            var columnCount = (int)pair.DoubleValue;
                            _readColumnCount = true;
                        }
                    }
                    else
                    {
                        RotationAngle = pair.DoubleValue;
                    }

                    break;
                case 63:
                    this.BackgroundFillColor = DxfColor.FromRawValue(pair.ShortValue);
                    break;
                case 71:
                    this.AttachmentPoint = (DxfAttachmentPoint)(pair.ShortValue);
                    break;
                case 72:
                    this.DrawingDirection = (DxfDrawingDirection)(pair.ShortValue);
                    break;
                case 73:
                    this.LineSpacingStyle = (DxfMTextLineSpacingStyle)(pair.ShortValue);
                    break;
                case 75:
                    this.ColumnType = (pair.ShortValue);
                    _readingColumnData = true;
                    break;
                case 76:
                    this.ColumnCount = (int)(pair.ShortValue);
                    break;
                case 78:
                    this.IsColumnFlowReversed = BoolShort(pair.ShortValue);
                    break;
                case 79:
                    this.IsColumnAutoHeight = BoolShort(pair.ShortValue);
                    break;
                case 90:
                    this.BackgroundFillSetting = (DxfBackgroundFillSetting)(pair.IntegerValue);
                    break;
                case 210:
                    this.ExtrusionDirection.X = pair.DoubleValue;
                    break;
                case 220:
                    this.ExtrusionDirection.Y = pair.DoubleValue;
                    break;
                case 230:
                    this.ExtrusionDirection.Z = pair.DoubleValue;
                    break;
                case 420:
                case 421:
                case 422:
                case 423:
                case 424:
                case 425:
                case 426:
                case 427:
                case 428:
                case 429:
                    this.BackgroundColorRGB = (pair.IntegerValue);
                    break;
                case 430:
                case 431:
                case 432:
                case 433:
                case 434:
                case 435:
                case 436:
                case 437:
                case 438:
                case 439:
                    this.BackgroundColorName = (pair.StringValue);
                    break;
                case 441:
                    this.BackgroundFillColorTransparency = (pair.IntegerValue);
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
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

    public partial class DxfPolyline : IDxfHasChildrenWithHandle
    {
        public new double Elevation
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

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var vertex in Vertices)
            {
                pairs.AddRange(vertex.GetValuePairs(version, outputHandles));
            }

            if (Seqend != null)
            {
                pairs.AddRange(Seqend.GetValuePairs(version, outputHandles));
            }
        }

        IEnumerable<IDxfHasHandle> IDxfHasChildrenWithHandle.GetChildren()
        {
            foreach (var vertex in vertices)
            {
                if (vertex != null)
                {
                    yield return vertex;
                }
            }

            if (seqend != null)
            {
                yield return seqend;
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

    public partial class DxfInsert : IDxfHasChildrenWithHandle
    {
        private List<DxfAttribute> attributes = new List<DxfAttribute>();
        private DxfSeqend seqend = new DxfSeqend();

        public List<DxfAttribute> Attributes { get { return attributes; } }

        public DxfSeqend Seqend
        {
            get { return seqend; }
            set { seqend = value; }
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var attribute in Attributes)
            {
                pairs.AddRange(attribute.GetValuePairs(version, outputHandles));
            }

            if (Seqend != null)
            {
                pairs.AddRange(Seqend.GetValuePairs(version, outputHandles));
            }
        }

        IEnumerable<IDxfHasHandle> IDxfHasChildrenWithHandle.GetChildren()
        {
            foreach (var attribute in attributes)
            {
                if (attribute != null)
                {
                    yield return attribute;
                }
            }

            if (seqend != null)
            {
                yield return seqend;
            }
        }
    }

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
            Debug.Assert((VertexCount == VertexCoordinateX.Count) && (VertexCount == VertexCoordinateY.Count));
            // TODO: how to read optional starting/ending width and bulge in this way?
            vertices.AddRange(VertexCoordinateX.Zip(VertexCoordinateY, (x, y) => new DxfLwPolylineVertex() { Location = new DxfPoint(x, y, 0.0) }));
            return this;
        }
    }

    public partial class DxfMLine
    {
        public List<DxfPoint> Vertices { get; } = new List<DxfPoint>();
        public List<DxfVector> SegmentDirections { get; } = new List<DxfVector>();
        public List<DxfVector> MiterDirections { get; } = new List<DxfVector>();

        protected override DxfEntity PostParse()
        {
            Debug.Assert(VertexCount == VertexX.Count && VertexCount == VertexY.Count && VertexCount == VertexZ.Count);
            for (int i = 0; i < VertexCount; i++)
            {
                Vertices.Add(new DxfPoint(VertexX[i], VertexY[i], VertexZ[i]));
            }

            VertexX.Clear();
            VertexY.Clear();
            VertexZ.Clear();

            Debug.Assert(VertexCount == SegmentDirectionX.Count && VertexCount == SegmentDirectionY.Count && VertexCount == SegmentDirectionY.Count);
            for (int i = 0; i < VertexCount; i++)
            {
                Vertices.Add(new DxfPoint(SegmentDirectionX[i], SegmentDirectionY[i], SegmentDirectionY[i]));
            }

            SegmentDirectionX.Clear();
            SegmentDirectionY.Clear();
            SegmentDirectionY.Clear();

            Debug.Assert(VertexCount == MiterDirectionX.Count && VertexCount == MiterDirectionY.Count && VertexCount == MiterDirectionZ.Count);
            for (int i = 0; i < VertexCount; i++)
            {
                Vertices.Add(new DxfPoint(MiterDirectionX[i], MiterDirectionY[i], MiterDirectionZ[i]));
            }

            MiterDirectionX.Clear();
            MiterDirectionY.Clear();
            MiterDirectionZ.Clear();

            return this;
        }
    }

    public partial class DxfEntitySection
    {
        public List<DxfPoint> Vertices { get; } = new List<DxfPoint>();
        public List<DxfPoint> BackLineVertices { get; } = new List<DxfPoint>();

        protected override DxfEntity PostParse()
        {
            Debug.Assert(VertexCount == VertexX.Count && VertexCount == VertexY.Count && VertexCount == VertexZ.Count);
            for (int i = 0; i < VertexCount; i++)
            {
                Vertices.Add(new DxfPoint(VertexX[i], VertexY[i], VertexZ[i]));
            }

            VertexX.Clear();
            VertexY.Clear();
            VertexZ.Clear();

            Debug.Assert(BackLineVertexCount == BackLineVertexX.Count && BackLineVertexCount == BackLineVertexY.Count && BackLineVertexCount == BackLineVertexZ.Count);
            for (int i = 0; i < BackLineVertexCount; i++)
            {
                BackLineVertices.Add(new DxfPoint(BackLineVertexX[i], BackLineVertexY[i], BackLineVertexZ[i]));
            }

            BackLineVertexX.Clear();
            BackLineVertexY.Clear();
            BackLineVertexZ.Clear();

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

    public partial class DxfUnderlay
    {
        public List<DxfPoint> BoundaryPoints { get; } = new List<DxfPoint>();

        protected override DxfEntity PostParse()
        {
            Debug.Assert(PointX.Count == PointY.Count);
            for (int i = 0; i < PointX.Count; i++)
            {
                BoundaryPoints.Add(new DxfPoint(PointX[i], PointY[i], 0.0));
            }

            PointX.Clear();
            PointY.Clear();

            return this;
        }
    }
}
