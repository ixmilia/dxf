// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfPolyline : IDxfItemInternal
    {
        #region IDxfItem and IDxfItemInternal
        uint IDxfItemInternal.Handle { get; set; }
        uint IDxfItemInternal.OwnerHandle { get; set; }

        void IDxfItemInternal.SetOwner(IDxfItem owner)
        {
            SetOwner(owner);
        }

        IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()
        {
            foreach (var pointer in _vertices.Pointers)
            {
                yield return pointer;
            }

            yield return _seqendPointer;
        }
        #endregion

        public new double Elevation
        {
            get { return Location.Z; }
            set { Location.Z = value; }
        }

        private DxfPointerList<DxfVertex> _vertices = new DxfPointerList<DxfVertex>();
        private DxfPointer _seqendPointer = new DxfPointer(new DxfSeqend());

        public IList<DxfVertex> Vertices { get { return _vertices; } }

        public DxfSeqend Seqend
        {
            get { return (DxfSeqend)_seqendPointer.Item; }
            set { _seqendPointer.Item = value; }
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            foreach (var vertex in Vertices)
            {
                pairs.AddRange(vertex.GetValuePairs(version, outputHandles));
            }

            if (Seqend != null)
            {
                pairs.AddRange(Seqend.GetValuePairs(version, outputHandles));
            }
        }
    }
}
