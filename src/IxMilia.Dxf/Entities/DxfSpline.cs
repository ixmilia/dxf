// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using IxMilia.Dxf.Collections;

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

        public IList<DxfPoint> ControlPoints { get; } = new ListNonNull<DxfPoint>();

        public IList<DxfPoint> FitPoints { get; } = new ListNonNull<DxfPoint>();

        protected override DxfEntity PostParse()
        {
            Debug.Assert((_controlPointX.Count == _controlPointY.Count) && (_controlPointX.Count == _controlPointZ.Count));
            for (int i = 0; i < _controlPointX.Count; i++)
            {
                ControlPoints.Add(new DxfPoint(_controlPointX[i], _controlPointY[i], _controlPointZ[i]));
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
    }
}
