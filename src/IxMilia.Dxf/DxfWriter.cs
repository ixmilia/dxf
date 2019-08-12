// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace IxMilia.Dxf
{
    internal class DxfWriter
    {
        private StreamWriter textWriter = null;
        private BinaryWriter binWriter = null;
        private Stream fileStream = null;

        private bool asText = true;
        private DxfAcadVersion version;

        public DxfWriter(Stream stream, bool asText, DxfAcadVersion version)
        {
            fileStream = stream;
            this.asText = asText;
            this.version = version;
        }

        public void Open()
        {
            if (asText)
            {
                // always create writer as UTF8; the actual file version will determine if just ASCII is written
                textWriter = new StreamWriter(fileStream, new UTF8Encoding(false));
            }
            else
            {
                binWriter = new BinaryWriter(fileStream);
                binWriter.Write(GetAsciiBytes(DxfFile.BinarySentinel));
                binWriter.Write((byte)'\r');
                binWriter.Write((byte)'\n');
                binWriter.Write((byte)26);
                binWriter.Write((byte)0);
            }
        }

        public void Close()
        {
            WriteCodeValuePair(new DxfCodePair(0, DxfFile.EofText));
            if (textWriter != null)
            {
                textWriter.Flush();
            }
            if (binWriter != null)
            {
                binWriter.Flush();
            }
        }

        public void WriteCodeValuePair(DxfCodePair pair)
        {
            WriteCode(pair.Code);
            WriteValue(pair.Code, pair.Value);
        }

        public void WriteCodeValuePairs(IEnumerable<DxfCodePair> pairs)
        {
            foreach (var pair in pairs)
                WriteCodeValuePair(pair);
        }

        private void WriteCode(int code)
        {
            if (textWriter != null)
            {
                WriteLine(code.ToString(CultureInfo.InvariantCulture).PadLeft(3));
            }
            else if (binWriter != null)
            {
                if (version >= DxfAcadVersion.R13)
                {
                    // 2 byte codes
                    binWriter.Write((short)code);
                }
                else if (code >= 255)
                {
                    binWriter.Write((byte)255);
                    binWriter.Write((short)code);
                }
                else
                {
                    binWriter.Write((byte)code);
                }
            }
            else
            {
                throw new InvalidOperationException("No writer available");
            }
        }

        private void WriteValue(int code, object value)
        {
            var type = DxfCodePair.ExpectedType(code);
            if (type == typeof(string))
                WriteString((string)value);
            else if (type == typeof(double))
                WriteDouble((double)value);
            else if (type == typeof(short))
                WriteShort((short)value);
            else if (type == typeof(int))
                WriteInt((int)value);
            else if (type == typeof(long))
                WriteLong((long)value);
            else if (type == typeof(bool))
            {
                if (DxfCodePair.IsPotentialShortAsBool(code) && value.GetType() == typeof(short))
                    WriteShort((short)value);
                else
                    WriteBool((bool)value);
            }
            else
                throw new InvalidOperationException("No writer available");
        }

        private void WriteString(string value)
        {
            value = TransformControlCharacters(value ?? string.Empty);
            if (textWriter != null)
                WriteStringWithEncoding(value);
            else if (binWriter != null)
            {
                binWriter.Write(GetAsciiBytes(value));
                binWriter.Write((byte)0);
            }
        }

        private void WriteStringWithEncoding(string value)
        {
            if (version <= DxfAcadVersion.R2004)
            {
                value = EscapeUnicode(value);
            }

            WriteLine(value);
        }

        private static bool IsUnicodeCharacter(char c)
        {
            return c > 127;
        }

        private static bool HasUnicodeCharacter(string value)
        {
            foreach (var c in value)
            {
                if (IsUnicodeCharacter(c))
                {
                    return true;
                }
            }

            return false;
        }

        private static string EscapeUnicode(string value)
        {
            if (HasUnicodeCharacter(value))
            {
                var sb = new StringBuilder();
                foreach (var c in value)
                {
                    if (IsUnicodeCharacter(c))
                    {
                        sb.Append($"\\U+{(uint)c:X4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                return sb.ToString();
            }
            else
            {
                return value;
            }
        }

        private void WriteDouble(double value)
        {
            if (textWriter != null)
                WriteLine(value.ToString("0.0##############", CultureInfo.InvariantCulture));
            else if (binWriter != null)
                binWriter.Write(value);
        }

        private void WriteShort(short value)
        {
            if (textWriter != null)
                WriteLine(value.ToString(CultureInfo.InvariantCulture).PadLeft(6));
            else if (binWriter != null)
                binWriter.Write(value);
        }

        private void WriteInt(int value)
        {
            if (textWriter != null)
                WriteLine(value.ToString(CultureInfo.InvariantCulture).PadLeft(9));
            else if (binWriter != null)
                binWriter.Write(value);
        }

        private void WriteLong(long value)
        {
            if (textWriter != null)
                WriteLine(value.ToString(CultureInfo.InvariantCulture));
            else if (binWriter != null)
                binWriter.Write(value);
        }

        private void WriteBool(bool value)
        {
            if (version >= DxfAcadVersion.R13 && binWriter != null)
            {
                // post R13 binary files write bools as a single byte
                binWriter.Write((byte)(value ? 0x01 : 0x00));
            }
            else
            {
                WriteShort(value ? (short)1 : (short)0);
            }
        }

        private void WriteLine(string value)
        {
            textWriter.Write(value);
            textWriter.Write("\r\n");
        }

        private static byte[] GetAsciiBytes(string value)
        {
            var result = new byte[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                result[i] = (byte)value[i];
            }

            return result;
        }

        private static string TransformControlCharacters(string str)
        {
            var sb = new StringBuilder();
            foreach (var c in str)
            {
                if ((c >= 0x00 && c <= 0x1F) || c == '^')
                {
                    sb.Append('^');
                    sb.Append(TransformControlCharacter(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static char TransformControlCharacter(char c)
        {
            switch ((int)c)
            {
                case 0x00: return '@';
                case 0x01: return 'A';
                case 0x02: return 'B';
                case 0x03: return 'C';
                case 0x04: return 'D';
                case 0x05: return 'E';
                case 0x06: return 'F';
                case 0x07: return 'G';
                case 0x08: return 'H';
                case 0x09: return 'I';
                case 0x0A: return 'J';
                case 0x0B: return 'K';
                case 0x0C: return 'L';
                case 0x0D: return 'M';
                case 0x0E: return 'N';
                case 0x0F: return 'O';
                case 0x10: return 'P';
                case 0x11: return 'Q';
                case 0x12: return 'R';
                case 0x13: return 'S';
                case 0x14: return 'T';
                case 0x15: return 'U';
                case 0x16: return 'V';
                case 0x17: return 'W';
                case 0x18: return 'X';
                case 0x19: return 'Y';
                case 0x1A: return 'Z';
                case 0x1B: return '[';
                case 0x1C: return '\\';
                case 0x1D: return ']';
                case 0x1E: return '^';
                case 0x1F: return '_';
                case '^': return ' ';
                default:
                    return c;
            }
        }
    }
}
