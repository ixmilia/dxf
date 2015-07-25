// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace IxMilia.Dxf
{
    internal class DxfBinaryReader : IDxfCodePairReader
    {
        private BinaryReader _reader;

        public DxfBinaryReader(BinaryReader reader)
        {
            _reader = reader;

            // swallow next two characters
            var sub = reader.ReadChar();
            Debug.Assert(sub == 0x1A);
            var nul = reader.ReadChar();
            Debug.Assert(nul == 0x00);
        }

        public IEnumerable<DxfCodePair> GetCodePairs()
        {
            DxfCodePair pair;
            while ((pair = GetCodePair()) != null)
            {
                yield return pair;
            }
        }

        private DxfCodePair GetCodePair()
        {
            if (CanRead())
            {
                var codeOffset = (int)_reader.BaseStream.Position;
                var code = ReadCode();
                var expectedType = DxfCodePair.ExpectedType(code);
                DxfCodePair pair;
                if (expectedType == typeof(short))
                {
                    pair = new DxfCodePair(code, _reader.ReadInt16());
                }
                else if (expectedType == typeof(double))
                {
                    pair = new DxfCodePair(code, _reader.ReadDouble());
                }
                else if (expectedType == typeof(int))
                {
                    pair = new DxfCodePair(code, _reader.ReadInt32());
                }
                else if (expectedType == typeof(long))
                {
                    pair = new DxfCodePair(code, _reader.ReadInt64());
                }
                else if (expectedType == typeof(string))
                {
                    var sb = new StringBuilder();
                    for (int b = _reader.Read(); b != 0; b = _reader.Read())
                        sb.Append((char)b);
                    pair = new DxfCodePair(code, DxfReader.TransformControlCharacters(sb.ToString()));
                }
                else if (expectedType == typeof(bool))
                {
                    pair = new DxfCodePair(code, _reader.ReadInt16() != 0);
                }
                else
                {
                    throw new DxfReadException("Unsupported code " + code, codeOffset);
                }

                pair.Offset = codeOffset;
                return pair;
            }
            else
            {
                return null;
            }
        }

        private int ReadCode()
        {
            int code = _reader.ReadByte();
            if (code == 255)
            {
                code = _reader.ReadInt16();
            }

            return code;
        }

        private bool CanRead()
        {
            return _reader.BaseStream.Position < _reader.BaseStream.Length;
        }
    }
}
