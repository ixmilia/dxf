// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfUnderlay
    {
        public IList<DxfPoint> BoundaryPoints { get; } = new List<DxfPoint>();

        protected override DxfEntity PostParse()
        {
            Debug.Assert(_pointX.Count == _pointY.Count);
            for (int i = 0; i < _pointX.Count; i++)
            {
                BoundaryPoints.Add(new DxfPoint(_pointX[i], _pointY[i], 0.0));
            }

            _pointX.Clear();
            _pointY.Clear();

            return this;
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            return BoundaryPoints;
        }
    }
}
