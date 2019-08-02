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
        private bool _returnedCodePairs;
        private bool _isPostR13File;

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
                _returnedCodePairs = true;
                yield return pair;
            }
        }

        public void SetUtf8Reader()
        {
            // noop
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
                if (_isPostR13File)
                {
                    // after R13 bools are encoded as a single byte
                    if (!TryReadByte(out var value))
                    {
                        return null;
                    }

                    pair = new DxfCodePair(code, value != 0);
                }
                else
                {
                    if (!TryReadInt16(out var value))
                    {
                        return null;
                    }

                    pair = DxfCodePair.IsPotentialShortAsBool(code)
                        ? new DxfCodePair(code, value)
                        : new DxfCodePair(code, value != 0);
                }
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
            // read the first byte
            code = default(int);
            byte b;
            if (!TryReadByte(out b))
            {
                return false;
            }

            code = b;

            if (!_returnedCodePairs && code == 0 && TryPeekByte(out var nextByte) && nextByte == 0x00)
            {
                // The first code/pair in a binary file must be `0/SECTION`; if we're reading the first pair, the code
                // is `0`, and the next byte is NULL (empty string), then this must be a post R13 file where codes are
                // always encoded with 2 bytes.
                _isPostR13File = true;
            }

            // potentially read the second byte of the code
            if (_isPostR13File)
            {
                if (!TryReadByte(out var b2))
                {
                    return false;
                }

                code = CreateShort(b, b2);
            }
            else if (code == 255)
            {
                if (!TryReadInt16(out var extendedCode))
                {
                    return false;
                }

                code = extendedCode;
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

        private bool TryPeekByte(out byte result)
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

            result = _miniBuffer[_miniBufferStart];
            return true;
        }

        private bool TryReadByte(out byte result)
        {
            if (TryPeekByte(out result))
            {
                _miniBufferStart++;
                return true;
            }

            return false;
        }

        private short CreateShort(byte b1, byte b2)
        {
            _dataBuffer[0] = b1;
            _dataBuffer[1] = b2;
            return BitConverter.ToInt16(_dataBuffer, 0);
        }

        private bool TryReadInt16(out short result)
        {
            if (TryReadByte(out var b1) && TryReadByte(out var b2))
            {
                result = CreateShort(b1, b2);
                return true;
            }

            result = default(short);
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
