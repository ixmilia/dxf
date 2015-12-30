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
            var collected = GatherObjects(objects);
            section.Objects.AddRange(collected);
            return section;
        }

        internal static List<DxfObject> GatherObjects(IEnumerable<DxfObject> objects)
        {
            var result = new List<DxfObject>();
            var dictionaries = new List<DxfDictionary>();
            var variables = new Dictionary<uint, DxfDictionaryVariable>();
            foreach (var obj in objects)
            {
                switch (obj.ObjectType)
                {
                    case DxfObjectType.Dictionary:
                        dictionaries.Add((DxfDictionary)obj);
                        result.Add(obj);
                        break;
                    case DxfObjectType.DictionaryVariable:
                        variables[obj.Handle] = (DxfDictionaryVariable)obj;
                        break;
                    default:
                        result.Add(obj);
                        break;
                }
            }

            foreach (var dict in dictionaries)
            {
                foreach (var kvp in dict.Handles)
                {
                    if (variables.ContainsKey(kvp.Value))
                    {
                        dict[kvp.Key] = variables[kvp.Value].Value;
                    }
                    else
                    {
                        // TODO: dictionary values aren't just limited to strings from DxfDictionaryVariables; they can
                        // be any item with a handle
                    }
                }
            }

            return result;
        }
    }
}
