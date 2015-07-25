// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using IxMilia.Dxf.Entities;

namespace IxMilia.Dxf
{
    internal class DxbWriter
    {
        private BinaryWriter _writer;

        public DxbWriter(Stream stream)
        {
            _writer = new BinaryWriter(stream);
        }

        public void Save(DxfFile file)
        {
            // write sentinel "AutoCAD DXB 1.0 CR LF ^Z NUL"
            foreach (var c in DxbReader.BinarySentinel)
            {
                _writer.Write((byte)c);
            }

            _writer.Write((byte)'\r'); // CR
            _writer.Write((byte)'\n'); // LF
            _writer.Write((byte)0x1A); // ^Z
            _writer.Write((byte)0x00);

            var writingBlock = file.Entities.Count == 0 && file.Blocks.Count == 1;
            if (writingBlock)
            {
                // write block header
                WriteItemType(DxbItemType.BlockBase);
                var block = file.Blocks.Single();
                WriteN((float)block.BasePoint.X);
                WriteN((float)block.BasePoint.Y);
            }

            // force all numbers to be floats
            WriteItemType(DxbItemType.NumberMode);
            WriteW(1);

            if (writingBlock)
            {
                WriteEntities(file.Blocks.Single().Entities);
            }
            else
            {
                var layerGroups = file.Entities.GroupBy(e => e.Layer).OrderBy(g => g.Key);
                foreach (var group in layerGroups)
                {
                    var layerName = group.Key;
                    WriteItemType(DxbItemType.NewLayer);
                    foreach (var c in layerName)
                    {
                        _writer.Write((byte)c);
                    }

                    _writer.Write((byte)0.00); // null terminator for string
                    WriteEntities(group);
                }
            }

            // write null terminator
            _writer.Write((byte)0x00);
            _writer.Flush();
        }

        private void WriteEntities(IEnumerable<DxfEntity> entities)
        {
            foreach (var entity in entities)
            {
                switch (entity.EntityType)
                {
                    case DxfEntityType.Line:
                        WriteLine((DxfLine)entity);
                        break;
                    case DxfEntityType.Point:
                        WriteModelPoint((DxfModelPoint)entity);
                        break;
                    case DxfEntityType.Circle:
                        WriteCircle((DxfCircle)entity);
                        break;
                    case DxfEntityType.Arc:
                        WriteArc((DxfArc)entity);
                        break;
                    case DxfEntityType.Trace:
                        WriteTrace((DxfTrace)entity);
                        break;
                    case DxfEntityType.Solid:
                        WriteSolid((DxfSolid)entity);
                        break;
                    case DxfEntityType.Seqend:
                        WriteSeqend();
                        break;
                    case DxfEntityType.Polyline:
                        WritePolyline((DxfPolyline)entity);
                        break;
                    case DxfEntityType.Vertex:
                        WriteVertex((DxfVertex)entity);
                        break;
                    case DxfEntityType.Face:
                        WriteFace((Dxf3DFace)entity);
                        break;
                }
            }
        }

        private void WriteA(double angle)
        {
            _writer.Write((float)angle);
        }

        private void WriteN(double d)
        {
            _writer.Write((float)d);
        }

        private void WriteW(short s)
        {
            _writer.Write(s);
        }

        private void WriteItemType(DxbItemType itemType)
        {
            _writer.Write((byte)itemType);
        }

        private void WriteLine(DxfLine line)
        {
            WriteItemType(DxbItemType.Line);
            WriteN(line.P1.X);
            WriteN(line.P1.Y);
            WriteN(line.P1.Z);
            WriteN(line.P2.X);
            WriteN(line.P2.Y);
            WriteN(line.P2.Z);
        }

        private void WriteModelPoint(DxfModelPoint point)
        {
            WriteItemType(DxbItemType.Point);
            WriteN(point.Location.X);
            WriteN(point.Location.Y);
        }

        private void WriteCircle(DxfCircle circle)
        {
            WriteItemType(DxbItemType.Circle);
            WriteN(circle.Center.X);
            WriteN(circle.Center.Y);
            WriteN(circle.Radius);
        }

        private void WriteArc(DxfArc arc)
        {
            WriteItemType(DxbItemType.Arc);
            WriteN(arc.Center.X);
            WriteN(arc.Center.Y);
            WriteN(arc.Radius);
            WriteA(arc.StartAngle);
            WriteA(arc.EndAngle);
        }

        private void WriteTrace(DxfTrace trace)
        {
            WriteItemType(DxbItemType.Trace);
            WriteN(trace.FirstCorner.X);
            WriteN(trace.FirstCorner.Y);
            WriteN(trace.SecondCorner.X);
            WriteN(trace.SecondCorner.Y);
            WriteN(trace.ThirdCorner.X);
            WriteN(trace.ThirdCorner.Y);
            WriteN(trace.FourthCorner.X);
            WriteN(trace.FourthCorner.Y);
        }

        private void WriteSolid(DxfSolid solid)
        {
            WriteItemType(DxbItemType.Solid);
            WriteN(solid.FirstCorner.X);
            WriteN(solid.FirstCorner.Y);
            WriteN(solid.SecondCorner.X);
            WriteN(solid.SecondCorner.Y);
            WriteN(solid.ThirdCorner.X);
            WriteN(solid.ThirdCorner.Y);
            WriteN(solid.FourthCorner.X);
            WriteN(solid.FourthCorner.Y);
        }

        private void WriteSeqend()
        {
            WriteItemType(DxbItemType.Seqend);
        }

        private void WritePolyline(DxfPolyline polyline)
        {
            WriteItemType(DxbItemType.Polyline);
            WriteW((short)(polyline.IsClosed ? 1 : 0));
            foreach (var vertex in polyline.Vertices)
            {
                WriteVertex(vertex);
            }

            WriteSeqend();
        }

        private void WriteVertex(DxfVertex vertex)
        {
            WriteItemType(DxbItemType.Vertex);
            WriteN(vertex.Location.X);
            WriteN(vertex.Location.Y);
        }

        private void WriteFace(Dxf3DFace face)
        {
            WriteItemType(DxbItemType.Face);
            WriteN(face.FirstCorner.X);
            WriteN(face.FirstCorner.Y);
            WriteN(face.FirstCorner.Z);
            WriteN(face.SecondCorner.X);
            WriteN(face.SecondCorner.Y);
            WriteN(face.SecondCorner.Z);
            WriteN(face.ThirdCorner.X);
            WriteN(face.ThirdCorner.Y);
            WriteN(face.ThirdCorner.Z);
            WriteN(face.FourthCorner.X);
            WriteN(face.FourthCorner.Y);
            WriteN(face.FourthCorner.Z);
        }
    }
}
