using System;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf
{
    public struct DxfBoundingBox
    {
        public DxfPoint MinimumPoint { get; }
        public DxfVector Size { get; }

        public DxfPoint MaximumPoint => MinimumPoint + Size;

        public DxfBoundingBox(DxfPoint minimumPoint, DxfVector size)
        {
            MinimumPoint = minimumPoint;
            Size = size;
        }

        public DxfBoundingBox WithPoint(DxfPoint point)
        {
            var min = new DxfPoint(MinimumPoint.X, MinimumPoint.Y, MinimumPoint.Z); // copy the values so we don't accidentally update this
            var max = MaximumPoint;
            UpdateMinMax(ref min, ref max, point);
            return FromMinMax(min, max);
        }

        public DxfBoundingBox Combine(DxfBoundingBox other)
        {
            return FromPoints(new[] { MinimumPoint, MaximumPoint, other.MinimumPoint, other.MaximumPoint }).GetValueOrDefault();
        }

        public static DxfBoundingBox FromMinMax(DxfPoint minimum, DxfPoint maximum)
        {
            var size = maximum - minimum;
            return new DxfBoundingBox(minimum, size);
        }

        public static DxfBoundingBox? FromPoints(IEnumerable<DxfPoint> points)
        {
            if (points == null || !points.Any())
            {
                return null;
            }

            var cur = points.First();
            var min = new DxfPoint(cur.X, cur.Y, cur.Z); // copy the values so we don't accidentally update the underlying value
            var max = new DxfPoint(cur.X, cur.Y, cur.Z);
            foreach (var point in points)
            {
                UpdateMinMax(ref min, ref max, point);
            }

            var size = max - min;
            return new DxfBoundingBox(min, size);
        }

        private static void UpdateMinMax(ref DxfPoint min, ref DxfPoint max, DxfPoint point)
        {
            // min
            if (point.X < min.X)
            {
                min = new DxfPoint(point.X, min.Y, min.Z);
            }
            if (point.Y < min.Y)
            {
                min = new DxfPoint(min.X, point.Y, min.Z);
            }
            if (point.Z < min.Z)
            {
                min = new DxfPoint(min.X, min.Y, point.Z);
            }

            // max
            if (point.X > max.X)
            {
                max = new DxfPoint(point.X, max.Y, max.Z);
            }
            if (point.Y > max.Y)
            {
                max = new DxfPoint(max.X, point.Y, max.Z);
            }
            if (point.Z > max.Z)
            {
                max = new DxfPoint(max.X, max.Y, point.Z);
            }
        }
    }
}
