// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Objects;

namespace IxMilia.Dxf.Sections
{
    internal class DxfObjectsSection : DxfSection
    {
        public List<DxfObject> Objects { get; private set; }

        public DxfObjectsSection()
        {
            Objects = new List<DxfObject>();
        }

        public override DxfSectionType Type { get { return DxfSectionType.Objects; } }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version, bool outputHandles)
        {
            return Objects.SelectMany(o => o.GetValuePairs(version, outputHandles));
        }

        protected internal override void Clear()
        {
            Objects.Clear();
        }

        internal static DxfObjectsSection ObjectsSectionFromBuffer(DxfCodePairBufferReader buffer)
        {
            var objects = new List<DxfObject>();
            objects.Clear();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (DxfCodePair.IsSectionEnd(pair))
                {
                    // done reading objects
                    buffer.Advance(); // swallow (0, ENDSEC)
                    break;
                }

                if (pair.Code != 0)
                {
                    throw new DxfReadException("Expected new object.", pair);
                }

                var obj = DxfObject.FromBuffer(buffer);
                if (obj != null)
                {
                    objects.Add(obj);
                }
            }

            var section = new DxfObjectsSection();
            section.Objects.AddRange(objects);
            return section;
        }
    }
}
