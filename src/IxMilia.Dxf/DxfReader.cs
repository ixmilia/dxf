// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace IxMilia.Dxf
{
    internal class DxfReader
    {
        public Stream BaseStream { get; private set; }

        private bool readText = false;
        private string firstLine = null;
        private StreamReader textReader = null;
        private BinaryReader binReader = null;

        public DxfReader(Stream input)
        {
            this.BaseStream = input;
            binReader = new BinaryReader(input);
        }

        public IEnumerable<DxfCodePair> ReadCodePairs()
        {
            Initialize();
            var pair = ReadCodeValuePair();
            while (pair != null)
            {
                yield return pair;
                pair = ReadCodeValuePair();
            }
        }

        private void Initialize()
        {
            // read first line char-by-char
            var sb = new StringBuilder();
            char c = binReader.ReadChar();
            while (c != '\n')
            {
                sb.Append(c);
                c = binReader.ReadChar();
            }

            // trim BOM
            var line = sb.ToString();
            if (line.Length > 0 && line[0] == 0xFEFF)
            {
                line = line.Substring(1);
            }

            // if sentinel, continue with binary reader
            if (line.StartsWith(DxfFile.BinarySentinel))
            {
                readText = false;
                firstLine = null;

                // swallow next two characters
                var sub = binReader.ReadChar();
                Debug.Assert(sub == 0x1A);
                var nul = binReader.ReadChar();
                Debug.Assert(nul == 0x00);
            }
            else
            {
                // otherwise, first line is data
                readText = true;
                firstLine = line;
                textReader = new StreamReader(this.BaseStream);
            }
        }

        private DxfCodePair ReadCodeValuePair()
        {
            int code = ReadCode();
            if (code == -1)
                return null;
            var expectedType = DxfCodePair.ExpectedType(code);
            DxfCodePair pair;
            if (expectedType == typeof(short))
                pair = new DxfCodePair(code, ReadShort());
            else if (expectedType == typeof(double))
                pair = new DxfCodePair(code, ReadDouble());
            else if (expectedType == typeof(string))
                pair = new DxfCodePair(code, ReadString());
            else if (expectedType == typeof(int))
                pair = new DxfCodePair(code, ReadInt());
            else if (expectedType == typeof(long))
                pair = new DxfCodePair(code, ReadLong());
            else if (expectedType == typeof(bool))
                pair = new DxfCodePair(code, ReadBool());
            else
                throw new DxfReadException("Reading type not supported: " + expectedType);

            return pair;
        }

        private int ReadCode()
        {
            int code = 0;
            if (readText)
            {
                var line = ReadLine();
                if (line == null)
                {
                    code = -1;
                }
                else
                {
                    code = int.Parse(line.Trim());
                }
            }
            else
            {
                if (binReader.BaseStream.Position >= binReader.BaseStream.Length)
                {
                    code = -1;
                }
                else
                {
                    code = binReader.ReadByte();
                    if (code == 255)
                        code = binReader.ReadInt16();
                }
            }

            return code;
        }

        private short ReadShort()
        {
            return readText
                ? short.Parse(ReadLine().Trim())
                : binReader.ReadInt16();
        }

        private double ReadDouble()
        {
            return readText
                ? double.Parse(ReadLine().Trim())
                : binReader.ReadDouble();
        }

        private string ReadString()
        {
            if (readText)
            {
                return TransformControlCharacters(ReadLine().Trim());
            }
            else
            {
                var sb = new StringBuilder();
                for (int b = binReader.Read(); b != 0; b = binReader.Read())
                    sb.Append((char)b);
                return TransformControlCharacters(sb.ToString());
            }
        }

        private int ReadInt()
        {
            return readText
                ? int.Parse(ReadLine().Trim())
                : binReader.ReadInt32();
        }

        private long ReadLong()
        {
            return readText
                ? long.Parse(ReadLine().Trim())
                : binReader.ReadInt64();
        }

        private bool ReadBool()
        {
            return ReadShort() != 0;
        }

        private string ReadLine()
        {
            string result;
            if (firstLine != null)
            {
                result = firstLine;
                firstLine = null;
                return result;
            }

            result = textReader.ReadLine();
            return result;
        }

        private static string TransformControlCharacters(string str)
        {
            var start = str.IndexOf('^');
            if (start < 0)
            {
                return str;
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append(str.Substring(0, start));
                for (int i = start; i < str.Length; i++)
                {
                    if (str[i] == '^' && i < str.Length - 1)
                    {
                        var controlCharacter = str[i + 1];
                        sb.Append(TransformControlCharacter(controlCharacter));
                        i++;
                    }
                    else
                    {
                        sb.Append(str[i]);
                    }
                }

                return sb.ToString();
            }
        }

        private static char TransformControlCharacter(char c)
        {
            switch (c)
            {
                case '@': return (char)0x00;
                case 'A': return (char)0x01;
                case 'B': return (char)0x02;
                case 'C': return (char)0x03;
                case 'D': return (char)0x04;
                case 'E': return (char)0x05;
                case 'F': return (char)0x06;
                case 'G': return (char)0x07;
                case 'H': return (char)0x08;
                case 'I': return (char)0x09;
                case 'J': return (char)0x0A;
                case 'K': return (char)0x0B;
                case 'L': return (char)0x0C;
                case 'M': return (char)0x0D;
                case 'N': return (char)0x0E;
                case 'O': return (char)0x0F;
                case 'P': return (char)0x10;
                case 'Q': return (char)0x11;
                case 'R': return (char)0x12;
                case 'S': return (char)0x13;
                case 'T': return (char)0x14;
                case 'U': return (char)0x15;
                case 'V': return (char)0x16;
                case 'W': return (char)0x17;
                case 'X': return (char)0x18;
                case 'Y': return (char)0x19;
                case 'Z': return (char)0x1A;
                case '[': return (char)0x1B;
                case '\\': return (char)0x1C;
                case ']': return (char)0x1D;
                case '^': return (char)0x1E;
                case '_': return (char)0x1F;
                case ' ': return '^';
                default:
                    throw new DxfReadException("Unexpected ASCII control character: " + c);
            }
        }
    }
}
