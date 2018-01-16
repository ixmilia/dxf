// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf
{
    public struct DxfPoint : IEquatable<DxfPoint>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public DxfPoint(double x, double y, double z)
            : this()
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static implicit operator DxfVector(DxfPoint point)
        {
            return new DxfVector(point.X, point.Y, point.Z);
        }

        public static bool operator ==(DxfPoint p1, DxfPoint p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
        }

        public static bool operator !=(DxfPoint p1, DxfPoint p2)
        {
            return !(p1 == p2);
        }

        public static DxfPoint operator +(DxfPoint p1, DxfVector p2)
        {
            return new DxfPoint(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static DxfVector operator -(DxfPoint p1, DxfVector p2)
        {
            return new DxfVector(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static DxfPoint operator *(DxfPoint p, double scalar)
        {
            return new DxfPoint(p.X * scalar, p.Y * scalar, p.Z * scalar);
        }

        public static DxfPoint operator /(DxfPoint p, double scalar)
        {
            return new DxfPoint(p.X / scalar, p.Y / scalar, p.Z / scalar);
        }

        public override bool Equals(object obj)
        {
            return obj is DxfPoint && this == (DxfPoint)obj;
        }

        public bool Equals(DxfPoint other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", X, Y, Z);
        }

        public static DxfPoint Origin
        {
            get { return new DxfPoint(0, 0, 0); }
        }

        // the following methods are only used to allow setting individual x/y/z values in the auto-generated readers

        internal DxfPoint WithUpdatedX(double x)
        {
            return new DxfPoint(x, this.Y, this.Z);
        }

        internal DxfPoint WithUpdatedY(double y)
        {
            return new DxfPoint(this.X, y, this.Z);
        }

        internal DxfPoint WithUpdatedZ(double z)
        {
            return new DxfPoint(this.X, this.Y, z);
        }
    }
}
