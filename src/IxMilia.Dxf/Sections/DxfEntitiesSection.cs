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

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version)
        {
            return this.Entities.SelectMany(e => e.GetValuePairs(version));
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
                    throw new DxfReadException("Expected new entity.");
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

        private static List<DxfEntity> GatherEntities(IEnumerable<DxfEntity> entities)
        {
            var buffer = new DxfBufferReader<DxfEntity>(entities, (e) => e == null);
            var result = new List<DxfEntity>();
            while (buffer.ItemsRemain)
            {
                var entity = buffer.Peek();
                buffer.Advance();
                switch (entity.EntityType)
                {
                    case DxfEntityType.Insert:
                        var insert = (DxfInsert)entity;
                        if (insert.HasAttributes)
                        {
                            var attribs = CollectWhileType(buffer, DxfEntityType.Attribute).Cast<DxfAttribute>();
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

            return null;
        }
    }
}
