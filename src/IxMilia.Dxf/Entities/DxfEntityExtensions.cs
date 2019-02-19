// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf.Entities
{
    public static class DxfEntityExtensions
    {
        /// <summary>
        /// Gets a point corresponding to a specified angle of the given circle.
        /// </summary>
        /// <param name="angle">The angle in degrees.</param>
        public static DxfPoint GetPointFromAngle(this DxfCircle circle, double angle)
        {
            var sin = Math.Sin(angle * Math.PI / 180.0);
            var cos = Math.Cos(angle * Math.PI / 180.0);
            return new DxfPoint(cos, sin, 0.0) * circle.Radius + circle.Center;
        }

        public static bool ContainsAngle(this DxfArc arc, double angle)
        {
            // normalize all angles
            var start = NormalizeAngleDegree(arc.StartAngle);
            var end = NormalizeAngleDegree(arc.EndAngle);
            angle = NormalizeAngleDegree(angle);

            // simple case: CCW arc from small to large angle.
            if (start <= end)
            {
                return start <= angle && angle <= end;
            }
            // if end < start consider two areas
            return start <= angle && angle < 360 // start ccw ... 360°
                || 0 <= angle && angle <= end; // overflow: 0° ... end
        }

        private static double NormalizeAngleDegree(double angle)
        {
            if (angle >= 360)
            {
                angle -= 360;
            }
            else if (angle < 0)
            {
                angle += 360;
            }
            return angle;
        }

        /// <summary>
        /// Gets a point corresponding to a specified angle of the given ellipse.
        /// </summary>
        /// <param name="angle">The angle in radians.</param>
        public static DxfPoint GetPointFromAngle(this DxfEllipse ellipse, double angle)
        {
            var sin = Math.Sin(angle);
            var cos = Math.Cos(angle);
            var majorAxisLength = ellipse.MajorAxis.Length;
            var minorAxisLength = majorAxisLength * ellipse.MinorAxisRatio;
            return new DxfPoint(cos * majorAxisLength, sin * minorAxisLength, 0.0) + ellipse.Center;
        }

        public static bool ContainsAngle(this DxfEllipse ellipse, double angle)
        {
            var start = ellipse.StartParameter;
            var end = ellipse.EndParameter;

            // normalize angles such that start is always less than end
            if (start > end)
            {
                // ellipses specify angles in radians
                start -= Math.PI * 2.0;
            }

            return start <= angle && end >= angle;
        }

        public static DxfVector MinorAxis(this DxfEllipse ellipse)
        {
            var minor = ellipse.Normal.Cross(ellipse.MajorAxis);
            var minorUnit = minor / minor.Length;
            return minorUnit * ellipse.MajorAxis.Length * ellipse.MinorAxisRatio;
        }
    }
}
