// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace IxMilia.Dxf
{
    internal class DxfAsciiReader : IDxfCodePairReader
    {
        private IEnumerator<string> _lineEnumerator;
        private int _lineNumber;

        public DxfAsciiReader(Stream stream, string firstLine)
        {
            _lineEnumerator = GetLines(stream, firstLine).GetEnumerator();
        }

        public IEnumerable<DxfCodePair> GetCodePairs()
        {
            DxfCodePair pair;
            while ((pair = GetCodePair()) != null)
            {
                yield return pair;
            }
        }

        private IEnumerable<string> GetLines(Stream stream, string firstLine)
        {
            _lineNumber = 1;
            yield return firstLine;
            var streamReader = new StreamReader(stream);
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                _lineNumber++;
                yield return line;
            }
        }

        private DxfCodePair GetCodePair()
        {
            if (_lineEnumerator.MoveNext())
            {
                var codeLine = _lineEnumerator.Current;
                var codeLineNumber = _lineNumber;
                int code;
                if (int.TryParse(codeLine, NumberStyles.Integer, CultureInfo.InvariantCulture, out code))
                {
                    if (_lineEnumerator.MoveNext())
                    {
                        DxfCodePair pair = null;
                        var valueLine = _lineEnumerator.Current;
                        var expectedType = DxfCodePair.ExpectedType(code);
                        if (expectedType == typeof(short))
                        {
                            pair = GetCodePair<short>(code, _lineEnumerator.Current, short.TryParse, NumberStyles.Integer, (c, v) => new DxfCodePair(c, v));
                        }
                        else if (expectedType == typeof(double))
                        {
                            pair = GetCodePair<double>(code, _lineEnumerator.Current, double.TryParse, NumberStyles.Float, (c, v) => new DxfCodePair(c, v));
                        }
                        else if (expectedType == typeof(int))
                        {
                            pair = GetCodePair<int>(code, _lineEnumerator.Current, int.TryParse, NumberStyles.Integer, (c, v) => new DxfCodePair(c, v));
                        }
                        else if (expectedType == typeof(long))
                        {
                            pair = GetCodePair<long>(code, _lineEnumerator.Current, long.TryParse, NumberStyles.Integer, (c, v) => new DxfCodePair(c, v));
                        }
                        else if (expectedType == typeof(string))
                        {
                            pair = new DxfCodePair(code, DxfReader.TransformControlCharacters(_lineEnumerator.Current.Trim()));
                        }
                        else if (expectedType == typeof(bool))
                        {
                            short result;
                            if (short.TryParse(_lineEnumerator.Current, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                            {
                                pair = new DxfCodePair(code, result != 0);
                            }
                            else
                            {
                                throw new DxfReadException("Unsupported value for code", _lineNumber);
                            }
                        }
                        else
                        {
                            throw new DxfReadException("Unsupported code " + code, codeLineNumber);
                        }

                        if (pair != null)
                        {
                            pair.Offset = codeLineNumber;
                        }

                        return pair;
                    }
                    else
                    {
                        throw new DxfReadException("Expected value", _lineNumber);
                    }
                }
                else
                {
                    throw new DxfReadException("Unexpected code value", _lineNumber);
                }
            }
            else
            {
                return null;
            }
        }

        private delegate bool ParseDelegate<T>(string s, NumberStyles style, IFormatProvider provider, out T result);
        private delegate DxfCodePair Creator<T>(int code, T value);

        private DxfCodePair GetCodePair<T>(int code, string s, ParseDelegate<T> parser, NumberStyles style, Creator<T> creator)
        {
            T result;
            if (parser(s, style, CultureInfo.InvariantCulture, out result))
            {
                return creator(code, result);
            }
            else
            {
                throw new DxfReadException("Unsupported value for code", _lineNumber);
            }
        }
    }
}
