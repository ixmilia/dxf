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

        private static List<DxfObject> GatherObjects(IEnumerable<DxfObject> objects)
        {
            var buffer = new DxfBufferReader<DxfObject>(objects, o => o == null);
            var result = new List<DxfObject>();
            var defaultObjectHandles = new HashSet<uint>();
            while (buffer.ItemsRemain)
            {
                var obj = buffer.Peek();
                buffer.Advance();
                switch (obj.ObjectType)
                {
                    case DxfObjectType.DictionaryWithDefault:
                        var dict = (DxfDictionaryWithDefault)obj;
                        if (dict.DefaultObjectPointer.Handle != 0u)
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
