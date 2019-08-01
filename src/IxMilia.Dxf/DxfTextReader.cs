// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using IxMilia.Dxf.Extensions;

namespace IxMilia.Dxf
{
    internal class DxfTextReader : IDxfCodePairReader
    {
        private IEnumerator<string> _lineEnumerator;
        private int _lineNumber;
        private Stream _stream;
        private StreamReader _utf8Reader;

        public DxfTextReader(Stream stream, string firstLine)
        {
            _stream = stream;
            _lineEnumerator = GetLines(firstLine).GetEnumerator();
        }

        public IEnumerable<DxfCodePair> GetCodePairs()
        {
            DxfCodePair pair;
            while ((pair = GetCodePair()) != null)
            {
                yield return pair;
            }
        }

        public void SetUtf8Reader()
        {
            _utf8Reader = new StreamReader(_stream, Encoding.GetEncoding("utf-8"));
        }

        private IEnumerable<string> GetLines(string firstLine)
        {
            _lineNumber = 1;
            yield return firstLine;

            string line;
            while ((line = ReadLine()) != null)
            {
                _lineNumber++;
                yield return line;
            }
        }

        private string ReadLine()
        {
            if (_utf8Reader == null)
            {
                return _stream.ReadLine(out var _);
            }
            else
            {
                return _utf8Reader.ReadLine();
            }
        }

        private DxfCodePair GetCodePair()
        {
            if (_lineEnumerator.MoveNext())
            {
                var codeLine = _lineEnumerator.Current;
                var codeLineNumber = _lineNumber;
                int code;
                if (string.IsNullOrEmpty(codeLine))
                {
                    // a blank line when expecting a code means we can't parse any further
                    return null;
                }
                else if (int.TryParse(codeLine, NumberStyles.Integer, CultureInfo.InvariantCulture, out code))
                {
                    if (_lineEnumerator.MoveNext())
                    {
                        DxfCodePair pair = null;
                        var valueLine = _lineEnumerator.Current;
                        var expectedType = DxfCodePair.ExpectedType(code);
                        if (expectedType == typeof(short))
                        {
                            pair = GetCodePair<short>(code, valueLine, short.TryParse, NumberStyles.Integer, (c, v) => new DxfCodePair(c, v), minValue: short.MinValue, maxValue: short.MaxValue);
                        }
                        else if (expectedType == typeof(double))
                        {
                            pair = GetCodePair<double>(code, valueLine, double.TryParse, NumberStyles.Float, (c, v) => new DxfCodePair(c, v));
                        }
                        else if (expectedType == typeof(int))
                        {
                            pair = GetCodePair<int>(code, valueLine, int.TryParse, NumberStyles.Integer, (c, v) => new DxfCodePair(c, v), minValue: int.MinValue, maxValue: int.MaxValue);
                        }
                        else if (expectedType == typeof(long))
                        {
                            pair = GetCodePair<long>(code, valueLine, long.TryParse, NumberStyles.Integer, (c, v) => new DxfCodePair(c, v), minValue: long.MinValue, maxValue: long.MaxValue);
                        }
                        else if (expectedType == typeof(string))
                        {
                            var value = valueLine.Trim();
                            if (_utf8Reader == null)
                            {
                                // read as ASCII, transform UTF codes
                                value = DxfReader.TransformUnicodeCharacters(value);
                            }

                            pair = new DxfCodePair(code, DxfReader.TransformControlCharacters(value));
                        }
                        else if (expectedType == typeof(bool))
                        {
                            // this should really parse as a short, but it could be anything
                            double result;
                            if (double.TryParse(valueLine, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                            {
                                if (result < short.MinValue)
                                {
                                    result = short.MinValue;
                                }

                                if (result > short.MaxValue)
                                {
                                    result = short.MaxValue;
                                }

                                pair = DxfCodePair.IsPotentialShortAsBool(code)
                                    ? new DxfCodePair(code, (short)result)
                                    : new DxfCodePair(code, result != 0.0);
                            }
                            else
                            {
                                throw new DxfReadException($"Unsupported value '{valueLine}' for code '{code}'", _lineNumber);
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

        private DxfCodePair GetCodePair<T>(int code, string s, ParseDelegate<T> parser, NumberStyles style, Creator<T> creator, T? minValue = null, T? maxValue = null)
            where T: struct
        {
            T result;
            if (parser(s, style, CultureInfo.InvariantCulture, out result))
            {
                return creator(code, result);
            }
            else
            {
                // Unable to parse as appropriate; fall back to parsing as a double, constraining the value, and
                // casting.  There is the possibility that the string failed to parse as a double and that we're
                // simply trying again (and we'll fail again), but given that the result of this case is an
                // exception then the duplicate parse is fine and shouldn't really have a runtime cost.
                double d;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                {
                    if (minValue.HasValue && d < Convert.ToDouble(minValue.GetValueOrDefault()))
                    {
                        d = Convert.ToDouble(minValue.GetValueOrDefault());
                    }

                    if (maxValue.HasValue && d > Convert.ToDouble(maxValue.GetValueOrDefault()))
                    {
                        d = Convert.ToDouble(maxValue.GetValueOrDefault());
                    }

                    return creator(code, (T)Convert.ChangeType(d, typeof(T)));
                }

                throw new DxfReadException($"Unsupported value '{s}' for code '{code}'", _lineNumber);
            }
        }
    }
}
