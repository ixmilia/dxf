// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace IxMilia.Dxf
{
    internal class DxfWriter
    {
        private StreamWriter textWriter = null;
        private BinaryWriter binWriter = null;
        private Stream fileStream = null;

        private bool asText = true;

        public DxfWriter(Stream stream, bool asText)
        {
            fileStream = stream;
            this.asText = asText;
        }

        public void Open()
        {
            if (asText)
            {
                textWriter = new StreamWriter(fileStream);
            }
            else
            {
                binWriter = new BinaryWriter(fileStream);
                binWriter.Write(GetAsciiBytes(DxfFile.BinarySentinel));
                binWriter.Write("\r\n");
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
                textWriter.WriteLine(code.ToString().PadLeft(3));
            }
            else if (binWriter != null)
            {
                if (code >= 255)
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
                throw new DxfReadException("No writer available");
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
                WriteBool((bool)value);
            else
                throw new DxfReadException("No writer available");
        }

        private void WriteString(string value)
        {
            if (textWriter != null)
                textWriter.WriteLine(value);
            else if (binWriter != null)
            {
                binWriter.Write(GetAsciiBytes(value));
                binWriter.Write((byte)0);
            }
        }

        private void WriteDouble(double value)
        {
            if (textWriter != null)
                textWriter.WriteLine(value.ToString("E16"));
            else if (binWriter != null)
                binWriter.Write(value);
        }

        private void WriteShort(short value)
        {
            if (textWriter != null)
                textWriter.WriteLine(value);
            else if (binWriter != null)
                binWriter.Write(value);
        }

        private void WriteInt(int value)
        {
            if (textWriter != null)
                textWriter.WriteLine(value);
            else if (binWriter != null)
                binWriter.Write(value);
        }

        private void WriteLong(long value)
        {
            if (textWriter != null)
                textWriter.WriteLine(value);
            else if (binWriter != null)
                binWriter.Write(value);
        }

        private void WriteBool(bool value)
        {
            WriteShort(value ? (short)1 : (short)0);
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
    }
}
