// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Entities;

namespace IxMilia.Dxf.Sections
{
    internal class DxfEntitiesSection : DxfSection
    {
        public List<DxfEntity> Entities { get; private set; }

        public DxfEntitiesSection()
        {
            Entities = new List<DxfEntity>();
        }

        public override DxfSectionType Type
        {
            get { return DxfSectionType.Entities; }
        }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version, bool outputHandles)
        {
            return this.Entities.SelectMany(e => e.GetValuePairs(version, outputHandles));
        }

        internal static DxfEntitiesSection EntitiesSectionFromBuffer(DxfCodePairBufferReader buffer)
        {
            var entities = new List<DxfEntity>();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (DxfCodePair.IsSectionEnd(pair))
                {
                    // done reading entities
                    buffer.Advance(); // swallow (0, ENDSEC)
                    break;
                }

                if (pair.Code != 0)
                {
                    throw new DxfReadException("Expected new entity.", pair);
                }

                var entity = DxfEntity.FromBuffer(buffer);
                if (entity != null)
                {
                    entities.Add(entity);
                }
            }

            var section = new DxfEntitiesSection();
            var collected = GatherEntities(entities);
            section.Entities.AddRange(collected);
            return section;
        }

        internal static List<DxfEntity> GatherEntities(IEnumerable<DxfEntity> entities)
        {
            var buffer = new DxfBufferReader<DxfEntity>(entities, (e) => e == null);
            var result = new List<DxfEntity>();
            while (buffer.ItemsRemain)
            {
                var entity = buffer.Peek();
                buffer.Advance();
                switch (entity.EntityType)
                {
                    case DxfEntityType.Attribute:
                        var att = (DxfAttribute)entity;
                        att.MText = GetMText(buffer);
                        break;
                    case DxfEntityType.AttributeDefinition:
                        var attdef = (DxfAttributeDefinition)entity;
                        attdef.MText = GetMText(buffer);
                        break;
                    case DxfEntityType.Insert:
                        var insert = (DxfInsert)entity;
                        if (insert.HasAttributes)
                        {
                            var attribs = new List<DxfAttribute>();
                            while (buffer.ItemsRemain)
                            {
                                var nextAtt = GetNextAttribute(buffer);
                                if (nextAtt == null) break;
                                attribs.Add(nextAtt);
                            }

                            insert.Attributes.AddRange(attribs);
                            insert.Seqend = GetSeqend(buffer);
                        }

                        break;
                    case DxfEntityType.Polyline:
                        var poly = (DxfPolyline)entity;
                        var verts = CollectWhileType(buffer, DxfEntityType.Vertex).Cast<DxfVertex>();
                        poly.Vertices.AddRange(verts);
                        poly.Seqend = GetSeqend(buffer);
                        break;
                    default:
                        break;
                }

                result.Add(entity);
            }

            return result;
        }

        private static IEnumerable<DxfEntity> CollectWhileType(DxfBufferReader<DxfEntity> buffer, DxfEntityType type)
        {
            var result = new List<DxfEntity>();
            while (buffer.ItemsRemain)
            {
                var entity = buffer.Peek();
                if (entity.EntityType != type)
                    break;
                buffer.Advance();
                result.Add(entity);
            }

            return result;
        }

        private static DxfAttribute GetNextAttribute(DxfBufferReader<DxfEntity> buffer)
        {
            if (buffer.ItemsRemain)
            {
                var entity = buffer.Peek();
                if (entity.EntityType == DxfEntityType.Attribute)
                {
                    buffer.Advance();
                    var attribute = (DxfAttribute)entity;
                    attribute.MText = GetMText(buffer);
                    return attribute;
                }
            }

            return null;
        }

        private static DxfMText GetMText(DxfBufferReader<DxfEntity> buffer)
        {
            if (buffer.ItemsRemain)
            {
                var entity = buffer.Peek();
                if (entity.EntityType == DxfEntityType.MText)
                {
                    buffer.Advance();
                    return (DxfMText)entity;
                }
            }

            return new DxfMText();
        }

        private static DxfSeqend GetSeqend(DxfBufferReader<DxfEntity> buffer)
        {
            if (buffer.ItemsRemain)
            {
                var entity = buffer.Peek();
                if (entity.EntityType == DxfEntityType.Seqend)
                {
                    buffer.Advance();
                    return (DxfSeqend)entity;
                }
            }

            return new DxfSeqend();
        }
    }
}
