// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            var pairs = new List<DxfCodePair>();
            var pair = ReadCodeValuePair();
            while (pair != null)
            {
                pairs.Add(pair);
                pair = ReadCodeValuePair();
            }

            return pairs;
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
                return ReadLine().Trim();
            }
            else
            {
                var sb = new StringBuilder();
                for (int b = binReader.Read(); b != 0; b = binReader.Read())
                    sb.Append((char)b);
                return sb.ToString();
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
    }
}
