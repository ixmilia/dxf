// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

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

        private List<DxfPoint> controlPoints = new List<DxfPoint>();
        public List<DxfPoint> ControlPoints
        {
            get { return controlPoints; }
        }

        private List<DxfPoint> fitPoints = new List<DxfPoint>();
        public List<DxfPoint> FitPoints
        {
            get { return fitPoints; }
        }

        protected override DxfEntity PostParse()
        {
            Debug.Assert((_controlPointX.Count == _controlPointY.Count) && (_controlPointX.Count == _controlPointZ.Count));
            for (int i = 0; i < _controlPointX.Count; i++)
            {
                controlPoints.Add(new DxfPoint(_controlPointX[i], _controlPointY[i], _controlPointZ[i]));
            }

            _controlPointX.Clear();
            _controlPointY.Clear();
            _controlPointZ.Clear();

            Debug.Assert((_fitPointX.Count == _fitPointY.Count) && (_fitPointX.Count == _fitPointZ.Count));
            for (int i = 0; i < _fitPointX.Count; i++)
            {
                fitPoints.Add(new DxfPoint(_fitPointX[i], _fitPointY[i], _fitPointZ[i]));
            }

            _fitPointX.Clear();
            _fitPointY.Clear();
            _fitPointZ.Clear();

            return this;
        }
    }
}
