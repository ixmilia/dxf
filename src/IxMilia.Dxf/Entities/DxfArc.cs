using System;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfArc
    {
        public static bool TryCreateFromVertices(double x1, double y1, double bulge, double x2, double y2, out DxfArc arc)
        {
            // To the best of my knowledge, 3D Arcs are not defined via z but a normal vector.
            // Thus, simply ignore non-zero z values.

            if (Math.Abs(bulge) < 1e-10)
            {
                //throw new ArgumentException("arc bulge too small: " + bulge);
                arc = null;
                return false;
            }

            var deltaX = x2 - x1;
            var deltaY = y2 - y1;
            var deltaLength = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (deltaLength < 1e-10)
            {
                //throw new ArgumentException("arc startpoint == endpoint");
                arc = null;
                return false;
            }

            var alpha = 4.0 * Math.Atan(bulge);
            var radius = deltaLength / (2.0 * Math.Abs(Math.Sin(alpha * 0.5)));
            var deltaNormX = deltaX / deltaLength;
            var deltaNormY = deltaY / deltaLength;
            int bulgeSign = Math.Sign(bulge);

            // 2D only solution (z=0)
            var normalX = -deltaNormY * bulgeSign;
            var normalY = +deltaNormX * bulgeSign;

            var centerX = (x1 + x2) * 0.5 + normalX * Math.Cos(alpha * 0.5) * radius;
            var centerY = (y1 + y2) * 0.5 + normalY * Math.Cos(alpha * 0.5) * radius;

            // bulge<0 indicates CW arc, but DxfArc is CCW always
            double startAngleDeg;
            double endAngleDeg;
            if (bulge > 0)
            {
                startAngleDeg = NormalizedAngleDegree(x1, y1, centerX, centerY);
                endAngleDeg = NormalizedAngleDegree(x2, y2, centerX, centerY);
            }
            else
            {
                endAngleDeg = NormalizedAngleDegree(x1, y1, centerX, centerY);
                startAngleDeg = NormalizedAngleDegree(x2, y2, centerX, centerY);
            }

            arc = new DxfArc(new DxfPoint(centerX, centerY, 0), radius, startAngleDeg, endAngleDeg);
            return true;
        }

        public static bool TryCreateFromVertices(DxfVertex v1, DxfVertex v2, out DxfArc arc)
        {
            return TryCreateFromVertices(v1.Location.X, v1.Location.Y, v1.Bulge, v2.Location.X, v2.Location.Y, out arc);
        }
        
        public static bool TryCreateFromVertices(DxfLwPolylineVertex v1, DxfLwPolylineVertex v2, out DxfArc arc)
        {
            return TryCreateFromVertices(v1.X, v1.Y, v1.Bulge, v2.X, v2.Y, out arc);
        }

        private static double NormalizedAngleDegree(double pX, double pY, double centerX, double centerY)
        {
            var dx = pX - centerX;
            var dy = pY - centerY;
            var angle = Math.Atan2(dy, dx);
            if (angle < 0)
            {
                angle += 2 * Math.PI;
            }

            return angle * 180 / Math.PI;
        }
    }
}