using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Collections;
using IxMilia.Dxf.Objects;

namespace IxMilia.Dxf.Sections
{
    internal class DxfObjectsSection : DxfSection
    {
        public IList<DxfObject> Objects { get; }

        public DxfObjectsSection()
        {
            Objects = new ListNonNull<DxfObject>();
        }

        public override DxfSectionType Type { get { return DxfSectionType.Objects; } }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version, bool outputHandles, HashSet<IDxfItem> writtenItems)
        {
            foreach (var obj in Objects)
            {
                if (writtenItems.Add(obj))
                {
                    foreach (var pair in obj.GetValuePairs(version, outputHandles, writtenItems))
                    {
                        yield return pair;
                    }
                }
            }
        }

        protected internal override void Clear()
        {
            Objects.Clear();
        }

        internal void Normalize()
        {
            if (Objects.FirstOrDefault()?.ObjectType != DxfObjectType.Dictionary)
            {
                // first object must be a dictionary
                Objects.Insert(0, new DxfDictionary());
            }

            // now ensure that dictionary contains the expected values
            var dict = (DxfDictionary)Objects.First();
            if (!dict.ContainsKey("ACAD_GROUP") || !(dict["ACAD_GROUP"] is DxfDictionary))
            {
                dict["ACAD_GROUP"] = new DxfDictionary();
            }
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
            foreach (var obj in objects)
            {
                section.Objects.Add(obj);
            }

            return section;
        }

        private static List<DxfObject> GatherObjects(IEnumerable<DxfObject> objects)
        {
            var buffer = new DxfBufferReader<DxfObject>(objects, o => o == null);
            var result = new List<DxfObject>();
            var defaultObjectHandles = new HashSet<DxfHandle>();
            while (buffer.ItemsRemain)
            {
                var obj = buffer.Peek();
                buffer.Advance();
                switch (obj.ObjectType)
                {
                    case DxfObjectType.DictionaryWithDefault:
                        var dict = (DxfDictionaryWithDefault)obj;
                        if (dict.DefaultObjectPointer.Handle.Value != 0)
                        {
                            defaultObjectHandles.Add(dict.DefaultObjectPointer.Handle);
                        }
                        break;
                    default:
                        break;
                }

                result.Add(obj);
            }

            // trim default objects from the resultant list because they shouldn't be directly accessible
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (defaultObjectHandles.Contains(((IDxfItemInternal)result[i]).Handle))
                {
                    result.RemoveAt(i);
                }
            }

            return result;
        }
    }
}
