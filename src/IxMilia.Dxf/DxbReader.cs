// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Sections;

namespace IxMilia.Dxf
{
    internal class DxbReader
    {
        public const string BinarySentinel = "AutoCAD DXB 1.0";

        private bool _isIntegerMode = true;
        private string _layerName = "0";
        private double _scaleFactor = 1.0;
        private DxfColor _color = DxfColor.ByLayer;
        private DxfPoint _lastLinePoint = DxfPoint.Origin;
        private DxfPoint _lastTraceP3 = DxfPoint.Origin;
        private DxfPoint _lastTraceP4 = DxfPoint.Origin;

        public DxfFile ReadFile(BinaryReader reader)
        {
            // swallow next two characters
            var sub = reader.ReadChar();
            Debug.Assert(sub == 0x1A);
            var nul = reader.ReadChar();
            Debug.Assert(nul == 0x00);

            DxfPoint blockBase = null;
            var entities = new List<DxfEntity>();
            var stillReading = true;
            Action<Func<BinaryReader, DxfEntity>> addEntity = (entityReader) =>
            {
                var entity = entityReader(reader);
                AssignCommonValues(entity);
                entities.Add(entity);
            };
            Func<DxfVertex> getLastVertex = () => entities.LastOrDefault() as DxfVertex;
            while (stillReading)
            {
                var itemType = (DxbItemType)reader.ReadByte();
                switch (itemType)
                {
                    case DxbItemType.Line:
                        addEntity(ReadLine);
                        break;
                    case DxbItemType.Point:
                        addEntity(ReadPoint);
                        break;
                    case DxbItemType.Circle:
                        addEntity(ReadCircle);
                        break;
                    case DxbItemType.Arc:
                        addEntity(ReadArc);
                        break;
                    case DxbItemType.Trace:
                        addEntity(ReadTrace);
                        break;
                    case DxbItemType.Solid:
                        addEntity(ReadSolid);
                        break;
                    case DxbItemType.Seqend:
                        addEntity(ReadSeqend);
                        break;
                    case DxbItemType.Polyline:
                        addEntity(ReadPolyline);
                        break;
                    case DxbItemType.Vertex:
                        addEntity(ReadVertex);
                        break;
                    case DxbItemType.Face:
                        addEntity(ReadFace);
                        break;
                    case DxbItemType.ScaleFactor:
                        _scaleFactor = ReadF(reader);
                        break;
                    case DxbItemType.NewLayer:
                        var sb = new StringBuilder();
                        for (int b = reader.ReadByte(); b != 0; b = reader.ReadByte())
                            sb.Append((char)b);
                        _layerName = sb.ToString();
                        break;
                    case DxbItemType.LineExtension:
                        addEntity(ReadLineExtension);
                        break;
                    case DxbItemType.TraceExtension:
                        addEntity(ReadTraceExtension);
                        break;
                    case DxbItemType.BlockBase:
                        var x = ReadN(reader);
                        var y = ReadN(reader);
                        if (blockBase == null && entities.Count == 0)
                        {
                            // only if this is the first item encountered
                            blockBase = new DxfPoint(x, y, 0.0);
                        }
                        break;
                    case DxbItemType.Bulge:
                        {
                            var bulge = ReadU(reader);
                            var lastVertex = getLastVertex();
                            if (lastVertex != null)
                            {
                                lastVertex.Bulge = bulge;
                            }
                        }
                        break;
                    case DxbItemType.Width:
                        {
                            var startWidth = ReadN(reader);
                            var endWidth = ReadN(reader);
                            var lastVertex = getLastVertex();
                            if (lastVertex != null)
                            {
                                lastVertex.StartingWidth = startWidth;
                                lastVertex.EndingWidth = endWidth;
                            }
                        }
                        break;
                    case DxbItemType.NumberMode:
                        _isIntegerMode = ReadW(reader) == 0;
                        break;
                    case DxbItemType.NewColor:
                        _color = DxfColor.FromRawValue((short)ReadW(reader));
                        break;
                    case DxbItemType.LineExtension3D:
                        addEntity(ReadLineExtension3D);
                        break;
                    case 0:
                        stillReading = false;
                        break;
                }
            }

            var file = new DxfFile();
            foreach (var section in file.Sections)
            {
                section.Clear();
            }

            // collect the entities (e.g., polylines, etc.)
            entities = DxfEntitiesSection.GatherEntities(entities);

            if (blockBase != null)
            {
                // entities are all contained in a block
                var block = new DxfBlock();
                block.BasePoint = blockBase;
                block.Entities.AddRange(entities);
                file.Blocks.Add(block);
            }
            else
            {
                // just a normal collection of entities
                file.Entities.AddRange(entities);
            }

            return file;
        }

        private static IEnumerable<DxfEntity> CollectEntities(IEnumerable<DxfEntity> entities)
        {
            return entities;
        }

        private double ReadA(BinaryReader reader)
        {
            if (_isIntegerMode)
            {
                return reader.ReadInt32() * _scaleFactor / 1000000.0;
            }
            else
            {
                return reader.ReadSingle();
            }
        }

        private double ReadF(BinaryReader reader)
        {
            return reader.ReadDouble();
        }

        private int ReadL(BinaryReader reader)
        {
            return (int)(reader.ReadInt32() * _scaleFactor);
        }

        private double ReadN(BinaryReader reader)
        {
            if (_isIntegerMode)
            {
                return reader.ReadInt16() * _scaleFactor;
            }
            else
            {
                return reader.ReadSingle();
            }
        }

        private double ReadU(BinaryReader reader)
        {
            if (_isIntegerMode)
            {
                return reader.ReadInt32() * 65536 * _scaleFactor;
            }
            else
            {
                return reader.ReadSingle();
            }
        }

        private int ReadW(BinaryReader reader)
        {
            return (int)(reader.ReadInt16() * _scaleFactor);
        }

        private void AssignCommonValues(DxfEntity entity)
        {
            entity.Color = _color;
            entity.Layer = _layerName;
        }

        private DxfLine ReadLine(BinaryReader reader)
        {
            var fromX = ReadN(reader);
            var fromY = ReadN(reader);
            var fromZ = ReadN(reader);
            var toX = ReadN(reader);
            var toY = ReadN(reader);
            var toZ = ReadN(reader);
            var from = new DxfPoint(fromX, fromY, fromZ);
            var to = new DxfPoint(toX, toY, toZ);
            _lastLinePoint = to;
            return new DxfLine(from, to);
        }

        private DxfModelPoint ReadPoint(BinaryReader reader)
        {
            return new DxfModelPoint(new DxfPoint(ReadN(reader), ReadN(reader), 0.0));
        }

        private DxfCircle ReadCircle(BinaryReader reader)
        {
            var centerX = ReadN(reader);
            var centerY = ReadN(reader);
            var radius = ReadN(reader);
            return new DxfCircle(new DxfPoint(centerX, centerY, 0.0), radius);
        }

        private DxfArc ReadArc(BinaryReader reader)
        {
            var centerX = ReadN(reader);
            var centerY = ReadN(reader);
            var radius = ReadN(reader);
            var start = ReadA(reader);
            var end = ReadA(reader);
            return new DxfArc(new DxfPoint(centerX, centerY, 0.0), radius, start, end);
        }

        private DxfTrace ReadTrace(BinaryReader reader)
        {
            var x1 = ReadN(reader);
            var y1 = ReadN(reader);
            var x2 = ReadN(reader);
            var y2 = ReadN(reader);
            var x3 = ReadN(reader);
            var y3 = ReadN(reader);
            var x4 = ReadN(reader);
            var y4 = ReadN(reader);
            var trace = new DxfTrace()
            {
                FirstCorner = new DxfPoint(x1, y1, 0.0),
                SecondCorner = new DxfPoint(x2, y2, 0.0),
                ThirdCorner = new DxfPoint(x3, y3, 0.0),
                FourthCorner = new DxfPoint(x4, y4, 0.0)
            };
            _lastTraceP3 = trace.ThirdCorner;
            _lastTraceP4 = trace.FourthCorner;
            return trace;
        }

        private DxfSolid ReadSolid(BinaryReader reader)
        {
            var x1 = ReadN(reader);
            var y1 = ReadN(reader);
            var x2 = ReadN(reader);
            var y2 = ReadN(reader);
            var x3 = ReadN(reader);
            var y3 = ReadN(reader);
            var x4 = ReadN(reader);
            var y4 = ReadN(reader);
            return new DxfSolid()
            {
                FirstCorner = new DxfPoint(x1, y1, 0.0),
                SecondCorner = new DxfPoint(x2, y2, 0.0),
                ThirdCorner = new DxfPoint(x3, y3, 0.0),
                FourthCorner = new DxfPoint(x4, y4, 0.0)
            };
        }

        private DxfSeqend ReadSeqend(BinaryReader reader)
        {
            return new DxfSeqend();
        }

        private DxfPolyline ReadPolyline(BinaryReader reader)
        {
            return new DxfPolyline() { IsClosed = ReadW(reader) != 0 };
        }

        private DxfVertex ReadVertex(BinaryReader reader)
        {
            var x = ReadN(reader);
            var y = ReadN(reader);
            return new DxfVertex(new DxfPoint(x, y, 0.0));
        }

        private Dxf3DFace ReadFace(BinaryReader reader)
        {
            var x1 = ReadN(reader);
            var y1 = ReadN(reader);
            var z1 = ReadN(reader);
            var x2 = ReadN(reader);
            var y2 = ReadN(reader);
            var z2 = ReadN(reader);
            var x3 = ReadN(reader);
            var y3 = ReadN(reader);
            var z3 = ReadN(reader);
            var x4 = ReadN(reader);
            var y4 = ReadN(reader);
            var z4 = ReadN(reader);
            return new Dxf3DFace()
            {
                FirstCorner = new DxfPoint(x1, y1, z1),
                SecondCorner = new DxfPoint(x2, y2, z2),
                ThirdCorner = new DxfPoint(x3, y3, z3),
                FourthCorner = new DxfPoint(x4, y4, z4)
            };
        }

        private DxfLine ReadLineExtension(BinaryReader reader)
        {
            var x = ReadN(reader);
            var y = ReadN(reader);
            var to = new DxfPoint(x, y, 0.0);
            var line = new DxfLine(_lastLinePoint, to);
            _lastLinePoint = to;
            return line;
        }

        private DxfTrace ReadTraceExtension(BinaryReader reader)
        {
            var x3 = ReadN(reader);
            var y3 = ReadN(reader);
            var x4 = ReadN(reader);
            var y4 = ReadN(reader);
            var trace = new DxfTrace()
            {
                FirstCorner = _lastTraceP3,
                SecondCorner = _lastTraceP4,
                ThirdCorner = new DxfPoint(x3, y3, 0.0),
                FourthCorner = new DxfPoint(x4, y4, 0.0)
            };
            _lastTraceP3 = trace.ThirdCorner;
            _lastTraceP4 = trace.FourthCorner;
            return trace;
        }

        private DxfLine ReadLineExtension3D(BinaryReader reader)
        {
            var x = ReadN(reader);
            var y = ReadN(reader);
            var z = ReadN(reader);
            var line = new DxfLine(_lastLinePoint, new DxfPoint(x, y, z));
            _lastLinePoint = line.P2;
            return line;
        }
    }
}
