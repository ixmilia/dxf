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
        private Encoding _encoding;

        public DxfTextReader(Stream stream, Encoding defaultEncoding, string firstLine)
        {
            _stream = stream;
            _encoding = defaultEncoding;
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
            _encoding = Encoding.UTF8;
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
            return _stream.ReadLine(_encoding, out var _);
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
                            pair = GetCodePair<short>(code, valueLine, TryParseShortValue, (c, v) => new DxfCodePair(c, v), minValue: short.MinValue, maxValue: short.MaxValue);
                        }
                        else if (expectedType == typeof(double))
                        {
                            pair = GetCodePair<double>(code, valueLine, TryParseDoubleValue, (c, v) => new DxfCodePair(c, v));
                        }
                        else if (expectedType == typeof(int))
                        {
                            pair = GetCodePair<int>(code, valueLine, TryParseIntValue, (c, v) => new DxfCodePair(c, v), minValue: int.MinValue, maxValue: int.MaxValue);
                        }
                        else if (expectedType == typeof(long))
                        {
                            pair = GetCodePair<long>(code, valueLine, TryParseLongValue, (c, v) => new DxfCodePair(c, v), minValue: long.MinValue, maxValue: long.MaxValue);
                        }
                        else if (expectedType == typeof(string))
                        {
                            var value = valueLine.Trim();
                            if (_encoding == null)
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
                    if (codeLineNumber == 1)
                    {
                        // parsing the very first code pair failed
                        throw new DxfReadException($"Not a valid DXF file header: `{codeLine}`.", _lineNumber);
                    }
                    else
                    {
                        throw new DxfReadException("Unexpected code value", _lineNumber);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        internal static bool TryParseShortValue(string s, out short result)
        {
            return short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        internal static bool TryParseDoubleValue(string s, out double result)
        {
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        internal static bool TryParseIntValue(string s, out int result)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        internal static bool TryParseLongValue(string s, out long result)
        {
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        internal static T ParseAndClampNumericValue<T>(string s, ParseDelegate<T> parser, T? minValue = null, T? maxValue = null, int code = 0, int lineNumber = 0)
            where T: struct
        {
            if (parser(s, out T result))
            {
                return result;
            }

            // Unable to parse as appropriate; fall back to parsing as a double, constraining the value, and
            // casting.  There is the possibility that the string failed to parse as a double and that we're
            // simply trying again (and we'll fail again), but given that the result of this case is an
            // exception then the duplicate parse is fine and shouldn't really have a runtime cost.
            if (TryParseDoubleValue(s, out var d))
            {
                if (minValue.HasValue && d < Convert.ToDouble(minValue.GetValueOrDefault()))
                {
                    d = Convert.ToDouble(minValue.GetValueOrDefault());
                }

                if (maxValue.HasValue && d > Convert.ToDouble(maxValue.GetValueOrDefault()))
                {
                    d = Convert.ToDouble(maxValue.GetValueOrDefault());
                }

                return (T)Convert.ChangeType(d, typeof(T));
            }

            throw new DxfReadException($"Unsupported value '{s}' for code '{code}'", lineNumber);
        }

        internal delegate bool ParseDelegate<T>(string s, out T result);
        private delegate DxfCodePair Creator<T>(int code, T value);

        private DxfCodePair GetCodePair<T>(int code, string s, ParseDelegate<T> parser, Creator<T> creator, T? minValue = null, T? maxValue = null)
            where T: struct
        {
            var value = ParseAndClampNumericValue(s, parser, minValue, maxValue, code, _lineNumber);
            return creator(code, value);
        }
    }
}
