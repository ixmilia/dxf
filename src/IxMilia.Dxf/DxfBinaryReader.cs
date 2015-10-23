// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace IxMilia.Dxf
{
    internal class DxfBinaryReader : IDxfCodePairReader
    {
        private BinaryReader _reader;

        public DxfBinaryReader(BinaryReader reader, int readBytes)
        {
            _reader = reader;
            _totalBytesRead = readBytes;

            // swallow next two characters
            var sub = reader.ReadByte();
            _totalBytesRead++;
            Debug.Assert(sub == 0x1A);

            var nul = reader.ReadByte();
            _totalBytesRead++;
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
            var codeOffset = _totalBytesRead + _miniBufferStart;
            int code;
            if (!TryReadCode(out code))
            {
                return null;
            }

            var expectedType = DxfCodePair.ExpectedType(code);
            DxfCodePair pair;
            if (expectedType == typeof(short))
            {
                short s;
                if (!TryReadInt16(out s))
                {
                    return null;
                }

                pair = new DxfCodePair(code, s);
            }
            else if (expectedType == typeof(double))
            {
                double d;
                if (!TryReadDouble(out d))
                {
                    return null;
                }

                pair = new DxfCodePair(code, d);
            }
            else if (expectedType == typeof(int))
            {
                int i;
                if (!TryReadInt32(out i))
                {
                    return null;
                }

                pair = new DxfCodePair(code, i);
            }
            else if (expectedType == typeof(long))
            {
                long l;
                if (!TryReadInt64(out l))
                {
                    return null;
                }

                pair = new DxfCodePair(code, l);
            }
            else if (expectedType == typeof(string))
            {
                var sb = new StringBuilder();
                byte b;
                for (; TryReadByte(out b) && b != 0;)
                {
                    sb.Append((char)b);
                }

                pair = new DxfCodePair(code, DxfReader.TransformControlCharacters(sb.ToString()));
            }
            else if (expectedType == typeof(bool))
            {
                bool b;
                if (!TryReadBool(out b))
                {
                    return null;
                }

                pair = new DxfCodePair(code, b);
            }
            else
            {
                throw new DxfReadException("Unsupported code " + code, codeOffset);
            }

            pair.Offset = codeOffset;
            return pair;
        }

        private bool TryReadCode(out int code)
        {
            code = default(int);
            byte b;
            if (!TryReadByte(out b))
            {
                return false;
            }

            if (b == 255)
            {
                short s;
                if (!TryReadInt16(out s))
                {
                    return false;
                }

                code = s;
            }
            else
            {
                code = b;
            }

            return true;
        }

        // Since we can't reliably call `_reader.BaseStream.Position` and `.Length`, we instead use the `Stream.Read(byte[], int, int)`
        // method to continually fill a local buffer.
        byte[] _miniBuffer = new byte[8];
        int _miniBufferStart = 0;
        int _miniBufferEnd = 0;
        int _totalBytesRead = 0;
        byte[] _dataBuffer = new byte[8];

        private bool TryReadByte(out byte result)
        {
            if (_miniBufferEnd - _miniBufferStart < 1)
            {
                _totalBytesRead += _miniBufferEnd;
                _miniBufferStart = 0;
                _miniBufferEnd = _reader.BaseStream.Read(_miniBuffer, 0, _miniBuffer.Length);
            }

            if (_miniBufferEnd == 0)
            {
                result = default(byte);
                return false;
            }

            result = _miniBuffer[_miniBufferStart++];
            return true;
        }

        private bool TryReadInt16(out short result)
        {
            if (TryReadByte(out _dataBuffer[0]) && TryReadByte(out _dataBuffer[1]))
            {
                result = BitConverter.ToInt16(_dataBuffer, 0);
                return true;
            }

            result = default(short);
            return false;
        }

        private bool TryReadBool(out bool result)
        {
            short s;
            if (TryReadInt16(out s))
            {
                result = s != 0;
                return true;
            }

            result = default(bool);
            return false;
        }

        private bool TryReadInt32(out int result)
        {
            if (TryReadByte(out _dataBuffer[0]) && TryReadByte(out _dataBuffer[1]) &&
                TryReadByte(out _dataBuffer[2]) && TryReadByte(out _dataBuffer[3]))
            {
                result = BitConverter.ToInt32(_dataBuffer, 0);
                return true;
            }

            result = default(int);
            return false;
        }

        private bool TryReadInt64(out long result)
        {
            if (TryReadByte(out _dataBuffer[0]) && TryReadByte(out _dataBuffer[1]) &&
                TryReadByte(out _dataBuffer[2]) && TryReadByte(out _dataBuffer[3]) &&
                TryReadByte(out _dataBuffer[4]) && TryReadByte(out _dataBuffer[5]) &&
                TryReadByte(out _dataBuffer[6]) && TryReadByte(out _dataBuffer[7]))
            {
                result = BitConverter.ToInt64(_dataBuffer, 0);
                return true;
            }

            result = default(long);
            return false;
        }

        private bool TryReadDouble(out double result)
        {
            if (TryReadByte(out _dataBuffer[0]) && TryReadByte(out _dataBuffer[1]) &&
                TryReadByte(out _dataBuffer[2]) && TryReadByte(out _dataBuffer[3]) &&
                TryReadByte(out _dataBuffer[4]) && TryReadByte(out _dataBuffer[5]) &&
                TryReadByte(out _dataBuffer[6]) && TryReadByte(out _dataBuffer[7]))
            {
                result = BitConverter.ToDouble(_dataBuffer, 0);
                return true;
            }

            result = default(double);
            return false;
        }
    }
}
