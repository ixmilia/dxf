using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Sections
{
    internal abstract class DxfSection
    {
        internal const string HeaderSectionText = "HEADER";
        internal const string ClassesSectionText = "CLASSES";
        internal const string TablesSectionText = "TABLES";
        internal const string BlocksSectionText = "BLOCKS";
        internal const string EntitiesSectionText = "ENTITIES";
        internal const string ObjectsSectionText = "OBJECTS";
        internal const string ThumbnailImageSectionText = "THUMBNAILIMAGE";

        internal const string SectionText = "SECTION";
        internal const string EndSectionText = "ENDSEC";

        internal const string TableText = "TABLE";
        internal const string EndTableText = "ENDTAB";

        public abstract DxfSectionType Type { get; }

        protected DxfSection()
        {
        }

        public override string ToString()
        {
            return Type.ToSectionName();
        }

        protected internal abstract IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version);

        internal IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version)
        {
            var pairs = GetSpecificPairs(version).ToList();
            if (pairs.Count == 0)
                yield break;
            yield return new DxfCodePair(0, SectionText);
            yield return new DxfCodePair(2, this.Type.ToSectionName());
            foreach (var pair in pairs)
                yield return pair;
            yield return new DxfCodePair(0, EndSectionText);
        }

        internal static DxfSection FromBuffer(DxfCodePairBufferReader buffer)
        {
            Debug.Assert(buffer.ItemsRemain);
            var sectionType = buffer.Peek();
            buffer.Advance();
            if (sectionType.Code != 2)
            {
                throw new DxfReadException("Expected code 2, got " + sectionType.Code);
            }

            DxfSection section;
            switch (sectionType.StringValue)
            {
                case BlocksSectionText:
                    section = DxfBlocksSection.BlocksSectionFromBuffer(buffer);
                    break;
                case ClassesSectionText:
                    section = DxfClassesSection.ClassesSectionFromBuffer(buffer);
                    break;
                case EntitiesSectionText:
                    section = DxfEntitiesSection.EntitiesSectionFromBuffer(buffer);
                    break;
                case HeaderSectionText:
                    section = DxfHeaderSection.HeaderSectionFromBuffer(buffer);
                    break;
                case TablesSectionText:
                    section = DxfTablesSection.TablesSectionFromBuffer(buffer);
                    break;
                case ThumbnailImageSectionText:
                    section = DxfThumbnailImageSection.ThumbnailImageSectionFromBuffer(buffer);
                    break;
                default:
                    SwallowSection(buffer);
                    section = null;
                    break;
            }

            return section;
        }

        internal static void SwallowSection(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                buffer.Advance();
                if (DxfCodePair.IsSectionEnd(pair))
                    break;
            }
        }
    }
}
