// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf
{
    public struct DxfVector : IEquatable<DxfVector>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public DxfVector(double x, double y, double z)
            : this()
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public double LengthSquared
        {
            get { return X * X + Y * Y + Z * Z; }
        }

        public double Length
        {
            get { return Math.Sqrt(LengthSquared); }
        }

        public bool IsZeroVector
        {
            get { return this.X == 0.0 && this.Y == 0.0 && this.Z == 0.0; }
        }

        public DxfVector Normalize()
        {
            return this / this.Length;
        }

        public DxfVector Cross(DxfVector v)
        {
            return new DxfVector(this.Y * v.Z - this.Z * v.Y, this.Z * v.X - this.X * v.Z, this.X * v.Y - this.Y * v.X);
        }

        public double Dot(DxfVector v)
        {
            return this.X * v.X + this.Y * v.Y + this.Z * v.Z;
        }

        public static implicit operator DxfPoint(DxfVector vector)
        {
            return new DxfPoint(vector.X, vector.Y, vector.Z);
        }

        public static DxfVector operator -(DxfVector vector)
        {
            return new DxfVector(-vector.X, -vector.Y, -vector.Z);
        }

        public static DxfVector operator +(DxfVector p1, DxfVector p2)
        {
            return new DxfVector(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static DxfVector operator -(DxfVector p1, DxfVector p2)
        {
            return new DxfVector(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static DxfVector operator *(DxfVector vector, double operand)
        {
            return new DxfVector(vector.X * operand, vector.Y * operand, vector.Z * operand);
        }

        public static DxfVector operator /(DxfVector vector, double operand)
        {
            return new DxfVector(vector.X / operand, vector.Y / operand, vector.Z / operand);
        }

        public static bool operator ==(DxfVector p1, DxfVector p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
        }

        public static bool operator !=(DxfVector p1, DxfVector p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            return obj is DxfVector && this == (DxfVector)obj;
        }

        public bool Equals(DxfVector other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }


        public bool IsParallelTo(DxfVector other)
        {
            return this.Cross(other).IsZeroVector;
        }

        public static DxfVector XAxis
        {
            get { return new DxfVector(1, 0, 0); }
        }

        public static DxfVector YAxis
        {
            get { return new DxfVector(0, 1, 0); }
        }

        public static DxfVector ZAxis
        {
            get { return new DxfVector(0, 0, 1); }
        }

        public static DxfVector Zero
        {
            get { return new DxfVector(0, 0, 0); }
        }

        public static DxfVector SixtyDegrees
        {
            get { return new DxfVector(0.5, Math.Sqrt(3.0) * 0.5, 0); }
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", X, Y, Z);
        }

        public static DxfVector RightVectorFromNormal(DxfVector normal)
        {
            if (normal == DxfVector.XAxis)
                return DxfVector.ZAxis;
            var right = DxfVector.XAxis;
            var up = normal.Cross(right);
            return up.Cross(normal).Normalize();
        }

        public static DxfVector NormalFromRightVector(DxfVector right)
        {
            // these two functions are identical, but the separate name makes them easier to understand
            return RightVectorFromNormal(right);
        }

        // the following methods are only used to allow setting individual x/y/z values in the auto-generated readers

        internal DxfVector WithUpdatedX(double x)
        {
            return new DxfVector(x, this.Y, this.Z);
        }

        internal DxfVector WithUpdatedY(double y)
        {
            return new DxfVector(this.X, y, this.Z);
        }

        internal DxfVector WithUpdatedZ(double z)
        {
            return new DxfVector(this.X, this.Y, z);
        }
    }
}
