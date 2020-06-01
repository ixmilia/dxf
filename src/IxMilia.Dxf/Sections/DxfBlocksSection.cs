using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Sections
{
    internal class DxfBlocksSection : DxfSection
    {
        public IList<DxfBlock> Blocks { get; }

        public DxfBlocksSection()
        {
            Blocks = new ListNonNull<DxfBlock>();
            Normalize();
        }

        internal void Normalize()
        {
            foreach (var name in new[] { "*MODEL_SPACE", "*PAPER_SPACE" })
            {
                if (!Blocks.Any(b => string.Compare(b.Name, name, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    Blocks.Add(new DxfBlock() { Name = name, Layer = "0" });
                }
            }
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

        internal static DxfBlocksSection BlocksSectionFromBuffer(DxfCodePairBufferReader buffer)
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
                var block = DxfBlock.FromBuffer(buffer);
                if (block != null)
                {
                    section.Blocks.Add(block);
                }
            }

            return section;
        }
    }
}
