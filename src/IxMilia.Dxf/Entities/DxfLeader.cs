using System;
using System.Collections.Generic;
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

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 3:
                    this.DimensionStyleName = pair.StringValue;
                    break;
                case 10:
                    // start a new vertex
                    this.Vertices.Add(new DxfPoint(pair.DoubleValue, 0.0, 0.0));
                    break;
                case 20:
                    // update last vertex
                    if (Vertices.Count > 0)
                    {
                        Vertices[Vertices.Count - 1] = Vertices[Vertices.Count - 1].WithUpdatedY(pair.DoubleValue);
                    }
                    break;
                case 30:
                    // update last vertex
                    if (Vertices.Count > 0)
                    {
                        Vertices[Vertices.Count - 1] = Vertices[Vertices.Count - 1].WithUpdatedZ(pair.DoubleValue);
                    }
                    break;
                case 40:
                    this.TextAnnotationHeight = pair.DoubleValue;
                    break;
                case 41:
                    this.TextAnnotationWidth = pair.DoubleValue;
                    break;
                case 71:
                    this.UseArrowheads = BoolShort(pair.ShortValue);
                    break;
                case 72:
                    this.PathType = (DxfLeaderPathType)pair.ShortValue;
                    break;
                case 73:
                    this.AnnotationType = (DxfLeaderCreationAnnotationType)pair.ShortValue;
                    break;
                case 74:
                    this.HooklineDirection = (DxfLeaderHooklineDirection)pair.ShortValue;
                    break;
                case 75:
                    this.UseHookline = BoolShort(pair.ShortValue);
                    break;
                case 76:
                    this._vertexCount = (int)pair.ShortValue;
                    break;
                case 77:
                    this.OverrideColor = DxfColor.FromRawValue(pair.ShortValue);
                    break;
                case 210:
                    this.Normal = this.Normal.WithUpdatedX(pair.DoubleValue);
                    break;
                case 220:
                    this.Normal = this.Normal.WithUpdatedY(pair.DoubleValue);
                    break;
                case 230:
                    this.Normal = this.Normal.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 211:
                    this.Right = this.Right.WithUpdatedX(pair.DoubleValue);
                    break;
                case 221:
                    this.Right = this.Right.WithUpdatedY(pair.DoubleValue);
                    break;
                case 231:
                    this.Right = this.Right.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 212:
                    this.BlockOffset = this.BlockOffset.WithUpdatedX(pair.DoubleValue);
                    break;
                case 222:
                    this.BlockOffset = this.BlockOffset.WithUpdatedY(pair.DoubleValue);
                    break;
                case 232:
                    this.BlockOffset = this.BlockOffset.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 213:
                    this.AnnotationOffset = this.AnnotationOffset.WithUpdatedX(pair.DoubleValue);
                    break;
                case 223:
                    this.AnnotationOffset = this.AnnotationOffset.WithUpdatedY(pair.DoubleValue);
                    break;
                case 233:
                    this.AnnotationOffset = this.AnnotationOffset.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 340:
                    this.AssociatedAnnotationReference = pair.StringValue;
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            return Vertices;
        }
    }
}
