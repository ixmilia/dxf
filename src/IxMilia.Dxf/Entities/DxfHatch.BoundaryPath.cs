using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfHatch
    {
        [Flags]
        public enum BoundaryPathType
        {
            Default = 0,
            External = 1,
            Polyline = 2,
            Derived = 4,
            Textbox = 8,
            Outermost = 16,
        }

        public abstract class BoundaryPathBase
        {
            public List<uint> BoundaryHandles { get; } = new List<uint>();

            internal virtual void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
            {
                pairs.Add(new DxfCodePair(97, BoundaryHandles.Count));
                foreach (var handle in BoundaryHandles)
                {
                    pairs.Add(new DxfCodePair(330, UIntHandle(handle)));
                }
            }

            internal virtual bool TrySetPair(DxfCodePair pair)
            {
                switch (pair.Code)
                {
                    case 97:
                        var _boundaryHandleCount = pair.IntegerValue;
                        break;
                    case 330:
                        BoundaryHandles.Add(UIntHandle(pair.StringValue));
                        break;
                    default:
                        return false;
                }

                return true;
            }

            internal static BoundaryPathBase CreateFromType(BoundaryPathType type)
            {
                if ((type & BoundaryPathType.Polyline) == BoundaryPathType.Polyline)
                {
                    // special polyline case
                    return new PolylineBoundaryPath();
                }

                return new NonPolylineBoundaryPath(type);
            }
        }

        public class NonPolylineBoundaryPath : BoundaryPathBase
        {
            public BoundaryPathType PathType { get; set; }
            public IList<BoundaryPathEdgeBase> Edges { get; } = new ListNonNull<BoundaryPathEdgeBase>();

            public NonPolylineBoundaryPath(BoundaryPathType pathType)
            {
                PathType = pathType;
            }

            internal override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
            {
                pairs.Add(new DxfCodePair(92, (int)PathType));
                pairs.Add(new DxfCodePair(93, Edges.Count));
                foreach (var edge in Edges)
                {
                    edge.AddValuePairs(pairs, version, outputHandles);
                }

                base.AddValuePairs(pairs, version, outputHandles);
            }

            internal override bool TrySetPair(DxfCodePair pair)
            {
                switch (pair.Code)
                {
                    case 93:
                        var _edgeCount = pair.IntegerValue;
                        break;
                    case 72:
                        switch (pair.ShortValue)
                        {
                            case 1:
                                Edges.Add(new LineBoundaryPathEdge());
                                break;
                            case 2:
                                Edges.Add(new CircularArcBoundaryPathEdge());
                                break;
                            case 3:
                                Edges.Add(new EllipticArcBoundaryPathEdge());
                                break;
                            case 4:
                                Edges.Add(new SplineBoundaryPathEdge());
                                break;
                            default:
                                return false;
                        }
                        break;
                    default:
                        if (base.TrySetPair(pair))
                        {
                            return true;
                        }
                        else
                        {
                            if (Edges.Count > 0)
                            {
                                return Edges.Last().TrySetPair(pair);
                            }

                            return false;
                        }
                }

                return true;
            }
        }

        public abstract class BoundaryPathEdgeBase
        {
            internal abstract void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles);
            internal abstract bool TrySetPair(DxfCodePair pair);
        }

        public class LineBoundaryPathEdge : BoundaryPathEdgeBase
        {
            public DxfPoint StartPoint { get; set; }
            public DxfPoint EndPoint { get; set; }

            internal override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
            {
                pairs.Add(new DxfCodePair(72, (short)1));
                pairs.Add(new DxfCodePair(10, StartPoint.X));
                pairs.Add(new DxfCodePair(20, StartPoint.Y));
                pairs.Add(new DxfCodePair(11, EndPoint.X));
                pairs.Add(new DxfCodePair(21, EndPoint.Y));
            }

            internal override bool TrySetPair(DxfCodePair pair)
            {
                switch (pair.Code)
                {
                    case 10:
                        StartPoint = StartPoint.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 20:
                        StartPoint = StartPoint.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 11:
                        EndPoint = EndPoint.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 21:
                        EndPoint = EndPoint.WithUpdatedY(pair.DoubleValue);
                        break;
                    default:
                        return false;
                }

                return true;
            }
        }

        public class CircularArcBoundaryPathEdge : BoundaryPathEdgeBase
        {
            public DxfPoint Center { get; set; }
            public double Radius { get; set; }
            public double StartAngle { get; set; }
            public double EndAngle { get; set; }
            public bool IsCounterClockwise { get; set; }

            internal override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
            {
                pairs.Add(new DxfCodePair(72, (short)2));
                pairs.Add(new DxfCodePair(10, Center.X));
                pairs.Add(new DxfCodePair(20, Center.Y));
                pairs.Add(new DxfCodePair(40, Radius));
                pairs.Add(new DxfCodePair(50, StartAngle));
                pairs.Add(new DxfCodePair(51, EndAngle));
                pairs.Add(new DxfCodePair(73, BoolShort(IsCounterClockwise)));
            }

            internal override bool TrySetPair(DxfCodePair pair)
            {
                switch (pair.Code)
                {
                    case 10:
                        Center = Center.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 20:
                        Center = Center.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 40:
                        Radius = pair.DoubleValue;
                        break;
                    case 50:
                        StartAngle = pair.DoubleValue;
                        break;
                    case 51:
                        EndAngle = pair.DoubleValue;
                        break;
                    case 73:
                        IsCounterClockwise = BoolShort(pair.ShortValue);
                        break;
                    default:
                        return false;
                }

                return true;
            }
        }

        public class EllipticArcBoundaryPathEdge : BoundaryPathEdgeBase
        {
            public DxfPoint Center { get; set; }
            public DxfVector MajorAxis { get; set; }
            public double MinorAxisRatio { get; set; }
            public double StartAngle { get; set; }
            public double EndAngle { get; set; }
            public bool IsCounterClockwise { get; set; }

            internal override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
            {
                pairs.Add(new DxfCodePair(72, (short)3));
                pairs.Add(new DxfCodePair(10, Center.X));
                pairs.Add(new DxfCodePair(20, Center.Y));
                pairs.Add(new DxfCodePair(11, MajorAxis.X));
                pairs.Add(new DxfCodePair(21, MajorAxis.Y));
                pairs.Add(new DxfCodePair(40, MinorAxisRatio));
                pairs.Add(new DxfCodePair(50, StartAngle));
                pairs.Add(new DxfCodePair(51, EndAngle));
                pairs.Add(new DxfCodePair(73, BoolShort(IsCounterClockwise)));
            }

            internal override bool TrySetPair(DxfCodePair pair)
            {
                switch (pair.Code)
                {
                    case 10:
                        Center = Center.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 20:
                        Center = Center.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 11:
                        MajorAxis = MajorAxis.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 21:
                        MajorAxis = MajorAxis.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 40:
                        MinorAxisRatio = pair.DoubleValue;
                        break;
                    case 50:
                        StartAngle = pair.DoubleValue;
                        break;
                    case 51:
                        EndAngle = pair.DoubleValue;
                        break;
                    case 73:
                        IsCounterClockwise = BoolShort(pair.ShortValue);
                        break;
                    default:
                        return false;
                }

                return true;
            }
        }

        public class SplineBoundaryPathEdge : BoundaryPathEdgeBase
        {
            public int Degree { get; set; }
            public bool IsRational { get; set; }
            public bool IsPeriodic { get; set; }
            public IList<double> Knots { get; } = new List<double>();
            public IList<DxfControlPoint> ControlPoints { get; } = new List<DxfControlPoint>();
            public IList<DxfPoint> FitPoints { get; } = new List<DxfPoint>();
            public DxfVector StartTangent { get; set; }
            public DxfVector EndTangent { get; set; }

            private int _currentWeight = 0;

            internal override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
            {
                pairs.Add(new DxfCodePair(72, (short)4));
                pairs.Add(new DxfCodePair(94, Degree));
                pairs.Add(new DxfCodePair(73, BoolShort(IsRational)));
                pairs.Add(new DxfCodePair(74, BoolShort(IsPeriodic)));
                pairs.Add(new DxfCodePair(95, Knots.Count));
                pairs.Add(new DxfCodePair(96, ControlPoints.Count));
                foreach (var item in Knots)
                {
                    pairs.Add(new DxfCodePair(40, item));
                }

                foreach (var item in ControlPoints)
                {
                    pairs.Add(new DxfCodePair(10, item.Point.X));
                    pairs.Add(new DxfCodePair(20, item.Point.Y));
                }

                if (ControlPoints.Any(cp => cp.Weight != 1.0))
                {
                    foreach (var item in ControlPoints)
                    {
                        pairs.Add(new DxfCodePair(42, item.Weight));
                    }
                }

                pairs.Add(new DxfCodePair(97, FitPoints.Count));
                foreach (var item in FitPoints)
                {
                    pairs.Add(new DxfCodePair(11, item.X));
                    pairs.Add(new DxfCodePair(21, item.Y));
                }

                pairs.Add(new DxfCodePair(12, StartTangent.X));
                pairs.Add(new DxfCodePair(22, StartTangent.Y));
                pairs.Add(new DxfCodePair(13, EndTangent.X));
                pairs.Add(new DxfCodePair(23, EndTangent.Y));
            }

            internal override bool TrySetPair(DxfCodePair pair)
            {
                switch (pair.Code)
                {
                    case 94:
                        Degree = pair.IntegerValue;
                        break;
                    case 73:
                        IsRational = BoolShort(pair.ShortValue);
                        break;
                    case 74:
                        IsPeriodic = BoolShort(pair.ShortValue);
                        break;
                    case 95:
                        var _knotCount = pair.IntegerValue;
                        break;
                    case 96:
                        var _controlPointCount = pair.IntegerValue;
                        break;
                    case 40:
                        Knots.Add(pair.DoubleValue);
                        break;
                    case 10:
                        ControlPoints.Add(new DxfControlPoint(new DxfPoint(pair.DoubleValue, 0.0, 0.0)));
                        break;
                    case 20:
                        ControlPoints[ControlPoints.Count - 1] = new DxfControlPoint(ControlPoints[ControlPoints.Count - 1].Point.WithUpdatedY(pair.DoubleValue));
                        break;
                    case 42:
                        ControlPoints[_currentWeight] = new DxfControlPoint(ControlPoints[_currentWeight].Point, pair.DoubleValue);
                        _currentWeight++;
                        break;
                    case 97:
                        var _fitPointCount = pair.IntegerValue;
                        break;
                    case 11:
                        FitPoints.Add(new DxfPoint(pair.DoubleValue, 0.0, 0.0));
                        break;
                    case 21:
                        FitPoints[FitPoints.Count - 1] = FitPoints[FitPoints.Count - 1].WithUpdatedY(pair.DoubleValue);
                        break;
                    case 12:
                        StartTangent = StartTangent.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 22:
                        StartTangent = StartTangent.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 13:
                        EndTangent = EndTangent.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 23:
                        EndTangent = EndTangent.WithUpdatedY(pair.DoubleValue);
                        break;
                    default:
                        return false;
                }

                return true;
            }
        }

        public class PolylineBoundaryPath : BoundaryPathBase
        {
            public bool IsClosed { get; set; }
            public List<DxfVertex> Vertices { get; } = new List<DxfVertex>();

            internal override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
            {
                pairs.Add(new DxfCodePair(92, 2)); // polyline type
                pairs.Add(new DxfCodePair(72, BoolShort(Vertices.Any(v => v.Bulge != 0.0))));
                pairs.Add(new DxfCodePair(73, BoolShort(IsClosed)));
                pairs.Add(new DxfCodePair(93, Vertices.Count));
                foreach (var vertex in Vertices)
                {
                    pairs.Add(new DxfCodePair(10, vertex.Location.X));
                    pairs.Add(new DxfCodePair(20, vertex.Location.Y));
                    if (vertex.Bulge != 0.0)
                    {
                        pairs.Add(new DxfCodePair(42, vertex.Bulge));
                    }
                }

                base.AddValuePairs(pairs, version, outputHandles);
            }

            internal override bool TrySetPair(DxfCodePair pair)
            {
                switch (pair.Code)
                {
                    case 72:
                        var _hasBulge = BoolShort(pair.ShortValue);
                        break;
                    case 73:
                        IsClosed = BoolShort(pair.ShortValue);
                        break;
                    case 93:
                        var _vertexCount = pair.IntegerValue;
                        break;
                    case 10:
                        Vertices.Add(new DxfVertex(new DxfPoint(pair.DoubleValue, 0.0, 0.0)));
                        break;
                    case 20:
                        Vertices.Last().Location = Vertices.Last().Location.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 42:
                        Vertices.Last().Bulge = pair.DoubleValue;
                        break;
                    default:
                        return base.TrySetPair(pair);
                }

                return true;
            }
        }
    }
}
