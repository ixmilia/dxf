// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfAcadProxyObject
    {
        public List<string> ObjectIds { get; } = new List<string>();

        public uint DrawingVersion
        {
            get { return _objectDrawingFormat | 0x0000FFFF; }
            set { _objectDrawingFormat |= value & 0x0000FFFF; }
        }

        public uint MaintenenceReleaseVersion
        {
            get { return (_objectDrawingFormat | 0xFFFF0000) >> 16; }
            set { _objectDrawingFormat |= (value & 0xFFFF0000) << 16; }
        }

        protected override DxfObject PostParse()
        {
            ObjectIds.AddRange(_objectIdsA);
            ObjectIds.AddRange(_objectIdsB);
            ObjectIds.AddRange(_objectIdsC);
            ObjectIds.AddRange(_objectIdsD);
            _objectIdsA.Clear();
            _objectIdsB.Clear();
            _objectIdsC.Clear();
            _objectIdsD.Clear();

            return this;
        }
    }
}
