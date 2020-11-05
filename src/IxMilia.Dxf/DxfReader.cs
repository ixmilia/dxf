using System;
using System.Globalization;
using System.Text;

namespace IxMilia.Dxf
{
    internal static class DxfReader
    {
        internal const string UnicodeMarker = @"\U+";
        internal const int UnicodeValueLength = 4;

        internal static string TransformUnicodeCharacters(string str)
        {
            var sb = new StringBuilder();
            int startIndex = 0;
            int currentIndex;
            while ((currentIndex = str.IndexOf(UnicodeMarker, startIndex)) >= 0)
            {
                var prefix = str.Substring(startIndex, currentIndex - startIndex);
                sb.Append(prefix);
                var unicode = str.Substring(currentIndex + UnicodeMarker.Length, UnicodeValueLength);
                if (int.TryParse(unicode, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                {
                    var c = char.ConvertFromUtf32(code);
                    sb.Append(c);
                }
                else
                {
                    // invalid unicode value
                    sb.Append('?');
                }

                startIndex = currentIndex + UnicodeMarker.Length + UnicodeValueLength;
            }

            sb.Append(str.Substring(startIndex));
            return sb.ToString();
        }

        internal static string TransformControlCharacters(string str)
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
                    throw new ArgumentOutOfRangeException(nameof(c), $"Unexpected ASCII control character: {c}");
            }
        }
    }
}
