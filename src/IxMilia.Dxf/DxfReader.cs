using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using IxMilia.Dxf.Sections;

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
            object value = ReadValue(DxfCodePair.ExpectedType(code));
            return new DxfCodePair(code, value);
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

        private object ReadValue(Type expectedType)
        {
            object value = null;
            if (readText)
            {
                string line = ReadLine().Trim(); ;
                if (expectedType == typeof(short))
                    value = short.Parse(line);
                else if (expectedType == typeof(double))
                    value = double.Parse(line);
                else if (expectedType == typeof(string))
                    value = line;
                else if (expectedType == typeof(int))
                    value = int.Parse(line);
                else if (expectedType == typeof(long))
                    value = long.Parse(line);
                else if (expectedType == typeof(bool))
                    value = short.Parse(line) != 0;
                else
                    throw new DxfReadException("Reading type not supported " + expectedType);
            }
            else
            {
                if (expectedType == typeof(short))
                    value = binReader.ReadInt16();
                else if (expectedType == typeof(double))
                    value = binReader.ReadDouble();
                else if (expectedType == typeof(string))
                {
                    StringBuilder sb = new StringBuilder();
                    for (int b = binReader.Read(); b != 0; b = binReader.Read())
                        sb.Append((char)b);
                    value = sb.ToString();
                }
                else if (expectedType == typeof(int))
                    value = binReader.ReadInt32();
                else if (expectedType == typeof(long))
                    value = binReader.ReadInt64();
                else if (expectedType == typeof(bool))
                    value = binReader.ReadInt16() != 0;
                else
                    throw new DxfReadException("Reading type not supported " + expectedType);
            }

            return value;
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
