// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfGeoData
    {
        public struct DxfGeoMeshPoint
        {
            public DxfPoint SourcePoint { get; set; }
            public DxfPoint DestinationPoint { get; set; }
        }

        public IList<DxfGeoMeshPoint> GeoMeshPoints { get; } = new List<DxfGeoMeshPoint>();
        public IList<DxfPoint> FaceIndices { get; } = new List<DxfPoint>();

        public IDxfItem HostBlock
        {
            get { return Owner; }
            set { ((IDxfItemInternal)this).SetOwner(value); }
        }

        protected override DxfObject PostParse()
        {
            // geo mesh points
            Debug.Assert(_geoMeshPointCount == _sourceMeshXPoints.Count);
            Debug.Assert(_geoMeshPointCount == _sourceMeshYPoints.Count);
            Debug.Assert(_geoMeshPointCount == _destinationMeshXPoints.Count);
            Debug.Assert(_geoMeshPointCount == _destinationMeshYPoints.Count);
            var limit = new[] { _geoMeshPointCount, _sourceMeshXPoints.Count, _sourceMeshYPoints.Count, _destinationMeshXPoints.Count, _destinationMeshYPoints.Count }.Min();
            GeoMeshPoints.Clear();
            for (int i = 0; i < limit; i++)
            {
                GeoMeshPoints.Add(new DxfGeoMeshPoint()
                {
                    SourcePoint = new DxfPoint(_sourceMeshXPoints[i], _sourceMeshYPoints[i], 0.0),
                    DestinationPoint = new DxfPoint(_destinationMeshXPoints[i], _destinationMeshYPoints[i], 0.0)
                });
            }

            _sourceMeshXPoints.Clear();
            _sourceMeshYPoints.Clear();
            _destinationMeshXPoints.Clear();
            _destinationMeshYPoints.Clear();

            // face index points
            Debug.Assert(_facesCount == _facePointIndexX.Count);
            Debug.Assert(_facesCount == _facePointIndexY.Count);
            Debug.Assert(_facesCount == _facePointIndexZ.Count);
            limit = new[] { _facesCount, _facePointIndexX.Count, _facePointIndexY.Count, _facePointIndexZ.Count }.Min();
            FaceIndices.Clear();
            for (int i = 0; i < limit; i++)
            {
                FaceIndices.Add(new DxfPoint(_facePointIndexX[i], _facePointIndexY[i], _facePointIndexZ[i]));
            }

            _facePointIndexX.Clear();
            _facePointIndexY.Clear();
            _facePointIndexZ.Clear();

            return this;
        }
    }
}
