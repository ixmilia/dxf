// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace IxMilia.Dxf.Objects
{
    public class DxfTransformationMatrix
    {
        public double M11 { get; set; }
        public double M12 { get; set; }
        public double M13 { get; set; }
        public double M14 { get; set; }
        public double M21 { get; set; }
        public double M22 { get; set; }
        public double M23 { get; set; }
        public double M24 { get; set; }
        public double M31 { get; set; }
        public double M32 { get; set; }
        public double M33 { get; set; }
        public double M34 { get; set; }
        public double M41 { get; set; }
        public double M42 { get; set; }
        public double M43 { get; set; }
        public double M44 { get; set; }

        public DxfTransformationMatrix(
            double m11, double m12, double m13, double m14,
            double m21, double m22, double m23, double m24,
            double m31, double m32, double m33, double m34,
            double m41, double m42, double m43, double m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;
            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;
            M41 = m41;
            M42 = m42;
            M43 = m43;
            M44 = m44;
        }

        internal DxfTransformationMatrix(params double[] values)
            : this(
                  GetIndexOrDefault(values, 0), GetIndexOrDefault(values, 1), GetIndexOrDefault(values, 2), GetIndexOrDefault(values, 3),
                  GetIndexOrDefault(values, 4), GetIndexOrDefault(values, 5), GetIndexOrDefault(values, 6), GetIndexOrDefault(values, 7),
                  GetIndexOrDefault(values, 8), GetIndexOrDefault(values, 9), GetIndexOrDefault(values, 10), GetIndexOrDefault(values, 11),
                  GetIndexOrDefault(values, 12), GetIndexOrDefault(values, 13), GetIndexOrDefault(values, 14), GetIndexOrDefault(values, 15))
        {
        }

        private static double GetIndexOrDefault(double[] values, int index)
        {
            return values.Length > index
                ? values[index]
                : default(double);
        }

        internal IEnumerable<double> GetValues()
        {
            yield return M11;
            yield return M12;
            yield return M13;
            yield return M14;
            yield return M21;
            yield return M22;
            yield return M23;
            yield return M24;
            yield return M31;
            yield return M32;
            yield return M33;
            yield return M34;
            yield return M41;
            yield return M42;
            yield return M43;
            yield return M44;
        }

        internal IEnumerable<double> Get4x3ValuesRowMajor()
        {
            // similar to GetValues() but it returns the 4x3 matrix values
            yield return M11;
            yield return M21;
            yield return M31;
            yield return M12;
            yield return M22;
            yield return M32;
            yield return M13;
            yield return M23;
            yield return M33;
            yield return M14;
            yield return M24;
            yield return M34;
        }

        public static DxfTransformationMatrix Identity
        {
            get
            {
                return new DxfTransformationMatrix(
                    1.0, 0.0, 0.0, 0.0,
                    0.0, 1.0, 0.0, 0.0,
                    0.0, 0.0, 1.0, 0.0,
                    0.0, 0.0, 0.0, 1.0);
            }
        }
    }
}
