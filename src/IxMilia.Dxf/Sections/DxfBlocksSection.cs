// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Blocks;

namespace IxMilia.Dxf.Sections
{
    internal class DxfBlocksSection : DxfSection
    {
        public List<DxfBlock> Blocks { get; private set; }

        public DxfBlocksSection()
        {
            Blocks = new List<DxfBlock>();
            Blocks.Add(new DxfBlock() { Name = "*MODEL_SPACE", Layer = "0" });
            Blocks.Add(new DxfBlock() { Name = "*PAPER_SPACE", Layer = "0" });
        }

        public override DxfSectionType Type
        {
            get { return DxfSectionType.Blocks; }
        }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
            foreach (var block in Blocks.Where(b => b != null))
            {
                if (writtenItems.Add(block))
                {
                    foreach (var pair in block.GetValuePairs(version, outputHandles))
                    {
                        yield return pair;
                    }
                }
            }
        }

        protected internal override void Clear()
        {
            Blocks.Clear();
        }

        internal static DxfBlocksSection BlocksSectionFromBuffer(DxfCodePairBufferReader buffer, DxfAcadVersion version)
        {
            var section = new DxfBlocksSection();
            section.Clear();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (DxfCodePair.IsSectionStart(pair))
                {
                    // done reading blocks, onto the next section
                    break;
                }
                else if (DxfCodePair.IsSectionEnd(pair))
                {
                    // done reading blocks
                    buffer.Advance(); // swallow (0, ENDSEC)
                    break;
                }

                if (pair.Code != 0)
                {
                    throw new DxfReadException("Expected new block.", pair);
                }

                buffer.Advance(); // swallow (0, CLASS)
                var block = DxfBlock.FromBuffer(buffer, version);
                if (block != null)
                {
                    section.Blocks.Add(block);
                }
            }

            return section;
        }
    }
}
