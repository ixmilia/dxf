// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Entities
{
    public class DxfLwPolylineVertex
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Identifier { get; set; }
        public double StartingWidth { get; set; }
        public double EndingWidth { get; set; }
        public double Bulge { get; set; }
    }
}
