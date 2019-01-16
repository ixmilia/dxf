// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Dxf.Collections;
using System.Linq;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfSpline
    {
        public int NumberOfKnots
        {
            get { return KnotValues.Count; }
        }

        public int NumberOfControlPoints
        {
            get { return ControlPoints.Count; }
        }

        public int NumberOfFitPoints
        {
            get { return FitPoints.Count; }
        }

        public IList<DxfPoint> ControlPoints { get; } = new List<DxfPoint>();

        public IList<DxfPoint> FitPoints { get; } = new List<DxfPoint>();

        protected override DxfEntity PostParse()
        {
            Debug.Assert((_controlPointX.Count == _controlPointY.Count) && (_controlPointX.Count == _controlPointZ.Count));
            for (int i = 0; i < _controlPointX.Count; i++)
            {
                ControlPoints.Add(new DxfPoint(_controlPointX[i], _controlPointY[i], _controlPointZ[i]));
            }

            if(Weights.Count != ControlPoints.Count)
            {
                Weights = ControlPoints.Select(curr => 1.0).ToArray();
            }
            
            _controlPointX.Clear();
            _controlPointY.Clear();
            _controlPointZ.Clear();

            Debug.Assert((_fitPointX.Count == _fitPointY.Count) && (_fitPointX.Count == _fitPointZ.Count));
            for (int i = 0; i < _fitPointX.Count; i++)
            {
                FitPoints.Add(new DxfPoint(_fitPointX[i], _fitPointY[i], _fitPointZ[i]));
            }

            _fitPointX.Clear();
            _fitPointY.Clear();
            _fitPointZ.Clear();

            return this;
        }

        protected override IEnumerable<DxfPoint> GetExtentsPoints()
        {
            // TODO: this doesn't account for the actual body of the curve; including `ControlPoints` would guarantee
            // that everything is contained, but at the cost of making the bounding box too big
            return FitPoints;
        }
    }
}
