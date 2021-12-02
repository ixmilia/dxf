using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfInsert : IDxfItemInternal
    {
        private DxfPointerList<DxfAttribute> _attributes = new DxfPointerList<DxfAttribute>();
        private DxfPointer _seqendPointer = new DxfPointer(new DxfSeqend());
        internal Func<IEnumerable<DxfEntity>> GetEntities { get; set; }

        public IEnumerable<DxfEntity> Entities => GetEntities?.Invoke();

        public IList<DxfAttribute> Attributes { get { return _attributes; } }

        public DxfSeqend Seqend
        {
            get { return _seqendPointer.Item as DxfSeqend; }
            set { _seqendPointer.Item = value; }
        }

        IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()
        {
            foreach (var att in _attributes.Pointers)
            {
                yield return att;
            }

            yield return _seqendPointer;
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var attribute in Attributes)
            {
                pairs.AddRange(attribute.GetValuePairs(version, outputHandles));
            }

            if (Attributes.Any() && Seqend != null)
            {
                pairs.AddRange(Seqend.GetValuePairs(version, outputHandles));
            }
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            if (GetEntities != null)
            {
                var boundingBoxes = GetEntities().Select(e => e.GetBoundingBox()).Where(bb => bb.HasValue).Select(bb => bb.GetValueOrDefault()).ToList();
                if (boundingBoxes.Count == 0)
                {
                    yield break;
                }

                var boundingBox = DxfBoundingBox.FromEnumerable(boundingBoxes);
                var minX = boundingBox.MinimumPoint.X * XScaleFactor + Location.X;
                var minY = boundingBox.MinimumPoint.Y * YScaleFactor + Location.Y;
                var minZ = boundingBox.MinimumPoint.Z * ZScaleFactor + Location.Z;
                var maxX = boundingBox.MaximumPoint.X * XScaleFactor + Location.X;
                var maxY = boundingBox.MaximumPoint.Y * YScaleFactor + Location.Y;
                var maxZ = boundingBox.MaximumPoint.Z * ZScaleFactor + Location.Z;
                var minP = new DxfPoint(minX, minY, minZ);
                var maxP = new DxfPoint(maxX, maxY, maxZ);
                yield return minP;
                yield return maxP;
            }
        }
    }
}
