// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Entities
{
    public struct DxfControlPoint
    {
        public DxfPoint Point { get; set; }
        public double Weight { get; set; }

        public DxfControlPoint(DxfPoint point, double weight)
        {
            Point = point;
            Weight = weight;
        }

        public DxfControlPoint(DxfPoint point)
            : this(point, 1.0)
        {
        }
    }
}
