using System.Collections.Generic;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfVertex
    {
        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            base.AddValuePairs(pairs, version, outputHandles);
            var subclassMarker = Is3DPolylineVertex || Is3DPolygonMesh ? "AcDb3dPolylineVertex" : "AcDb2dVertex";
            pairs.Add(new DxfCodePair(100, "AcDbVertex"));
            pairs.Add(new DxfCodePair(100, subclassMarker));
            pairs.Add(new DxfCodePair(10, Location.X));
            pairs.Add(new DxfCodePair(20, Location.Y));
            pairs.Add(new DxfCodePair(30, Location.Z));
            if (StartingWidth != 0.0)
            {
                pairs.Add(new DxfCodePair(40, StartingWidth));
            }

            if (EndingWidth != 0.0)
            {
                pairs.Add(new DxfCodePair(41, EndingWidth));
            }

            if (Bulge != 0.0)
            {
                pairs.Add(new DxfCodePair(42, Bulge));
            }

            pairs.Add(new DxfCodePair(70, (short)Flags));
            pairs.Add(new DxfCodePair(50, CurveFitTangentDirection));
            if (version >= DxfAcadVersion.R13)
            {
                if (PolyfaceMeshVertexIndex1 != 0)
                {
                    pairs.Add(new DxfCodePair(71, (short)PolyfaceMeshVertexIndex1));
                }

                if (PolyfaceMeshVertexIndex2 != 0)
                {
                    pairs.Add(new DxfCodePair(72, (short)PolyfaceMeshVertexIndex2));
                }

                if (PolyfaceMeshVertexIndex3 != 0)
                {
                    pairs.Add(new DxfCodePair(73, (short)PolyfaceMeshVertexIndex3));
                }

                if (PolyfaceMeshVertexIndex4 != 0)
                {
                    pairs.Add(new DxfCodePair(74, (short)PolyfaceMeshVertexIndex4));
                }
            }

            if (version >= DxfAcadVersion.R2010 && Identifier != 0)
            {
                pairs.Add(new DxfCodePair(91, Identifier));
            }
        }
    }
}
