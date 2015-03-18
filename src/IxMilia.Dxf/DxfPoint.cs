// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf
{
    public class DxfPoint
    {
        public double X;
        public double Y;
        public double Z;

        public DxfPoint()
            : this(0, 0, 0)
        {
        }

        public DxfPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", X, Y, Z);
        }

        public static DxfPoint Origin
        {
            get { return new DxfPoint(0, 0, 0); }
        }

        public static bool operator ==(DxfPoint a, DxfPoint b)
        {
            if (Object.ReferenceEquals(a, b))
                return true;
            if (((object)a) == null || ((object)b) == null)
                return false;
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(DxfPoint a, DxfPoint b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is DxfPoint)
                return this == (DxfPoint)obj;
            return false;
        }
    }

    public class DxfVector : DxfPoint
    {
        public DxfVector()
            : base()
        {
        }

        public DxfVector(double x, double y, double z)
            : base(x, y, z)
        {
        }

        public static DxfVector Zero
        {
            get { return new DxfVector(0, 0, 0); }
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
    }
}
