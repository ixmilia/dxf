using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfSpatialFilter
    {
        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            var hasReadFrontClippingPlane = false;
            var hasSetInverseMatrix = false;
            var matrixList = new List<double>();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                while (this.TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
                }

                if (pair.Code == 0)
                {
                    break;
                }

                switch (pair.Code)
                {
                    case 70:
                        var boundaryPointCount = pair.ShortValue;
                        break;
                    case 10:
                        // code 10 always starts a new point
                        ClipBoundaryDefinitionPoints.Add(new DxfPoint(pair.DoubleValue, 0.0, 0.0));
                        break;
                    case 20:
                        ClipBoundaryDefinitionPoints[ClipBoundaryDefinitionPoints.Count - 1] = ClipBoundaryDefinitionPoints[ClipBoundaryDefinitionPoints.Count - 1].WithUpdatedY(pair.DoubleValue);
                        break;
                    case 30:
                        ClipBoundaryDefinitionPoints[ClipBoundaryDefinitionPoints.Count - 1] = ClipBoundaryDefinitionPoints[ClipBoundaryDefinitionPoints.Count - 1].WithUpdatedZ(pair.DoubleValue);
                        break;
                    case 11:
                        ClipBoundaryOrigin = ClipBoundaryOrigin.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 21:
                        ClipBoundaryOrigin = ClipBoundaryOrigin.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 31:
                        ClipBoundaryOrigin = ClipBoundaryOrigin.WithUpdatedZ(pair.DoubleValue);
                        break;
                    case 40:
                        if (!hasReadFrontClippingPlane)
                        {
                            FrontClippingPlaneDistance = pair.DoubleValue;
                            hasReadFrontClippingPlane = true;
                        }
                        else
                        {
                            matrixList.Add(pair.DoubleValue);
                            if (matrixList.Count == 12)
                            {
                                var m11 = matrixList[0];
                                var m21 = matrixList[1];
                                var m31 = matrixList[2];
                                var m41 = 0.0;
                                var m12 = matrixList[3];
                                var m22 = matrixList[4];
                                var m32 = matrixList[5];
                                var m42 = 0.0;
                                var m13 = matrixList[6];
                                var m23 = matrixList[7];
                                var m33 = matrixList[8];
                                var m43 = 0.0;
                                var m14 = matrixList[9];
                                var m24 = matrixList[10];
                                var m34 = matrixList[11];
                                var m44 = 0.0;
                                var matrix = new DxfTransformationMatrix(
                                        m11, m12, m13, m14,
                                        m21, m22, m23, m24,
                                        m31, m32, m33, m34,
                                        m41, m42, m43, m44);
                                if (!hasSetInverseMatrix)
                                {
                                    InverseTransformationMatrix = matrix;
                                    hasSetInverseMatrix = true;
                                }
                                else
                                {
                                    TransformationMatrix = matrix;
                                }

                                matrixList.Clear();
                            }
                        }
                        break;
                    case 41:
                        BackClippingPlaneDistance = pair.DoubleValue;
                        break;
                    case 71:
                        IsClipBoundaryEnabled = BoolShort(pair.ShortValue);
                        break;
                    case 72:
                        IsFrontClippingPlane = BoolShort(pair.ShortValue);
                        break;
                    case 73:
                        IsBackClippingPlane = BoolShort(pair.ShortValue);
                        break;
                    case 210:
                        ClipBoundaryNormal = ClipBoundaryNormal.WithUpdatedX(pair.DoubleValue);
                        break;
                    case 220:
                        ClipBoundaryNormal = ClipBoundaryNormal.WithUpdatedY(pair.DoubleValue);
                        break;
                    case 230:
                        ClipBoundaryNormal = ClipBoundaryNormal.WithUpdatedZ(pair.DoubleValue);
                        break;
                    default:
                        if (!TrySetPair(pair))
                        {
                            InternalExcessCodePairs.Add(pair);
                        }
                        break;
                }

                buffer.Advance();
            }

            Debug.Assert(matrixList.Count == 0);
            return PostParse();
        }
    }
}
