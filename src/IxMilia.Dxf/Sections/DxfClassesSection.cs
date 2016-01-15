// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf.Sections
{
    internal class DxfClassesSection : DxfSection
    {
        public List<DxfClass> Classes { get; private set; }

        public DxfClassesSection()
        {
            Classes = new List<DxfClass>();
        }

        public override DxfSectionType Type
        {
            get { return DxfSectionType.Classes; }
        }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
            return this.Classes.SelectMany(e => e.GetValuePairs(version, outputHandles));
        }

        protected internal override void Clear()
        {
            Classes.Clear();
        }

        internal static DxfClassesSection ClassesSectionFromBuffer(DxfCodePairBufferReader buffer, DxfAcadVersion version)
        {
            var section = new DxfClassesSection();
            section.Clear();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (DxfCodePair.IsSectionEnd(pair))
                {
                    // done reading classes
                    buffer.Advance(); // swallow (0, ENDSEC)
                    break;
                }

                if (pair.Code != 0)
                {
                    throw new DxfReadException("Expected new class.", pair);
                }

                var cls = DxfClass.FromBuffer(buffer, version);
                if (cls != null)
                {
                    section.Classes.Add(cls);
                }
            }

            return section;
        }
    }
}
