using System;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfArc
    {
        public static bool TryCreateFromVertices(DxfVertex v1, DxfVertex v2, out DxfArc arc)
        {
            // To the best of my knowledge, 3D Arcs are not defined via z but a normal vector.
            // Thus, simply ignore non-zero z values.
            
            var bulge = v1.Bulge;
            if (Math.Abs(bulge) < 1e-10)
            {
                //throw new ArgumentException("arc bulge too small: " + bulge);
                arc = null;
                return false;
            }

            var startPoint = v1.Location;
            var endPoint = v2.Location;
            var delta = endPoint - startPoint;
            var length = delta.Length;
            if (length <= double.Epsilon)
            {
                //throw new ArgumentException("arc startpoint == endpoint");
                arc = null;
                return false;
            }

            var alpha = 4.0 * Math.Atan(bulge);
            var radius = length / (2.0 * Math.Abs(Math.Sin(alpha * 0.5)));
            var deltaNorm = delta.Normalize();
            int bulgeSign = Math.Sign(bulge);

            // 2D only solution (z=0)
            var normal = new DxfVector(-deltaNorm.Y, +deltaNorm.X, 0.0) * bulgeSign;
            var center = (startPoint + endPoint) * 0.5
                + normal * Math.Cos(alpha * 0.5) * radius;

            // bulge<0 indicates CW arc, but DxfArc is CCW always
            double startAngleDeg;
            double endAngleDeg;
            if (bulge > 0)
            {
                startAngleDeg = NormalizedAngleDegree(startPoint, center);
                endAngleDeg = NormalizedAngleDegree(endPoint, center);
            }
            else
            {
                endAngleDeg = NormalizedAngleDegree(startPoint, center);
                startAngleDeg = NormalizedAngleDegree(endPoint, center);
            }

            arc = new DxfArc(center, radius, startAngleDeg, endAngleDeg);
            return true;
        }

        private static double NormalizedAngleDegree(DxfPoint p, DxfPoint center)
        {
            var dx = p.X - center.X;
            var dy = p.Y - center.Y;
            var angle = Math.Atan2(dy, dx);
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }

            return angle * 180 / Math.PI;
        }
    }
}
