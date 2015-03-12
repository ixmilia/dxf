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

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version)
        {
           return this.Classes.SelectMany(e => e.GetValuePairs(version));
        }

        internal static DxfClassesSection ClassesSectionFromBuffer(DxfCodePairBufferReader buffer)
        {
            var section = new DxfClassesSection();
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
                    throw new DxfReadException("Expected new class.");
                }

                buffer.Advance(); // swallow (0, CLASS)
                var cls = DxfClass.FromBuffer(buffer);
                if (cls != null)
                {
                    section.Classes.Add(cls);
                }
            }

            return section;
        }
    }
}
