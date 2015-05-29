// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace IxMilia.Dxf.Sections
{
    internal class DxfObjectsSection : DxfSection
    {
        public override DxfSectionType Type { get { return DxfSectionType.Objects; } }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version, bool outputHandles)
        {
            yield break;
        }
    }
}
