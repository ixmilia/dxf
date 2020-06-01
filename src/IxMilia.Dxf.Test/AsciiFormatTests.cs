using System.Globalization;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class AsciiFormatTests : AbstractDxfTests
    {
        [Fact]
        public void NumericValueParserTests()
        {
            EnsureDoubleParse(11.0, "1.100000E+001");
            EnsureDoubleParse(55.0, "5.5e1");
            EnsureDoubleParse(2.0, "2");

            // some files encountered in the wild have double-like values for the integral types
            EnsureShortParse(2, "2.0");
            EnsureShortParse(short.MaxValue, "15e10");
            EnsureIntParse(int.MaxValue, "15e10");
            EnsureLongParse(long.MinValue, "-15e20");
        }

        [Fact]
        public void StringValueParserTests()
        {
            var actual = DxfReader.TransformControlCharacters("a^G^ ^^ b");
            var expected = "a\x7^\x1E b";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void VerifyStringEscapingTest()
        {
            var actual = DxfWriter.TransformControlCharacters("a\x7^\x1E b");
            var expected = "a^G^ ^^ b";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void VerifyCodeAsStringTest()
        {
            // codes written to ASCII files are right-padded for 3 spaces, overflow if too large
            Assert.Equal("  0", DxfWriter.CodeAsString(0));
            Assert.Equal(" 10", DxfWriter.CodeAsString(10));
            Assert.Equal("100", DxfWriter.CodeAsString(100));
            Assert.Equal("1000", DxfWriter.CodeAsString(1000));
        }

        [Fact]
        public void WriteShortTest()
        {
            // shorts are right-aligned to 6 spaces
            Assert.Equal("     1", DxfWriter.ShortAsString(1));
            Assert.Equal("    10", DxfWriter.ShortAsString(10));
            Assert.Equal("   100", DxfWriter.ShortAsString(100));
            Assert.Equal("  1000", DxfWriter.ShortAsString(1000));
            Assert.Equal(" 10000", DxfWriter.ShortAsString(10000));
            Assert.Equal("    -1", DxfWriter.ShortAsString(-1));
            Assert.Equal("   -10", DxfWriter.ShortAsString(-10));
            Assert.Equal("  -100", DxfWriter.ShortAsString(-100));
            Assert.Equal(" -1000", DxfWriter.ShortAsString(-1000));
            Assert.Equal("-10000", DxfWriter.ShortAsString(-10000));
        }

        [Fact]
        public void WriteIntTest()
        {
            // ints are right-aligned to 9 spaces
            Assert.Equal("        1", DxfWriter.IntAsString(1));
            Assert.Equal("       10", DxfWriter.IntAsString(10));
            Assert.Equal("      100", DxfWriter.IntAsString(100));
            Assert.Equal("     1000", DxfWriter.IntAsString(1000));
            Assert.Equal("    10000", DxfWriter.IntAsString(10000));
            Assert.Equal("   100000", DxfWriter.IntAsString(100000));
            Assert.Equal("  1000000", DxfWriter.IntAsString(1000000));
            Assert.Equal(" 10000000", DxfWriter.IntAsString(10000000));
            Assert.Equal("100000000", DxfWriter.IntAsString(100000000));
            Assert.Equal("       -1", DxfWriter.IntAsString(-1));
            Assert.Equal("      -10", DxfWriter.IntAsString(-10));
            Assert.Equal("     -100", DxfWriter.IntAsString(-100));
            Assert.Equal("    -1000", DxfWriter.IntAsString(-1000));
            Assert.Equal("   -10000", DxfWriter.IntAsString(-10000));
            Assert.Equal("  -100000", DxfWriter.IntAsString(-100000));
            Assert.Equal(" -1000000", DxfWriter.IntAsString(-1000000));
            Assert.Equal("-10000000", DxfWriter.IntAsString(-10000000));
            Assert.Equal("-100000000", DxfWriter.IntAsString(-100000000));
        }

        [Fact]
        public void WriteLongTest()
        {
            // longs are written as-is
            Assert.Equal("1", DxfWriter.LongAsString(1));
            Assert.Equal("10", DxfWriter.LongAsString(10));
            Assert.Equal("100", DxfWriter.LongAsString(100));
            Assert.Equal("-1", DxfWriter.LongAsString(-1));
        }

        [Fact]
        public void WriteDoubleTest()
        {
            Assert.Equal("1.0", DxfWriter.DoubleAsString(1.0));
            Assert.Equal("1.000005", DxfWriter.DoubleAsString(1.000005));
            Assert.Equal("-1.0", DxfWriter.DoubleAsString(-1.0));
            Assert.Equal("-1.000005", DxfWriter.DoubleAsString(-1.000005));
            Assert.Equal("1500000000000000.0", DxfWriter.DoubleAsString(1.5e15));
        }

        [Fact]
        public void ParseWithInvariantCultureTest()
        {
            // from https://github.com/ixmilia/dxf/issues/36
            var existingCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                EnsureDoubleParse(3.5, "3.5");
            }
            finally
            {
                CultureInfo.CurrentCulture = existingCulture;
            }
        }

        [Fact]
        public void WriteAsciiWithInvariantCultureTest()
        {
            var existingCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.Equal("3.5", DxfWriter.DoubleAsString(3.5));
            }
            finally
            {
                CultureInfo.CurrentCulture = existingCulture;
            }
        }

        private void EnsureDoubleParse(double expected, string s)
        {
            Assert.True(DxfTextReader.TryParseDoubleValue(s, out var actual));
            Assert.Equal(expected, actual);
        }

        private void EnsureShortParse(short expected, string s)
        {
            var actual = DxfTextReader.ParseAndClampNumericValue<short>(s, DxfTextReader.TryParseShortValue, short.MinValue, short.MaxValue);
            Assert.Equal(expected, actual);
        }

        private void EnsureIntParse(int expected, string s)
        {
            var actual = DxfTextReader.ParseAndClampNumericValue<int>(s, DxfTextReader.TryParseIntValue, int.MinValue, int.MaxValue);
            Assert.Equal(expected, actual);
        }

        private void EnsureLongParse(long expected, string s)
        {
            var actual = DxfTextReader.ParseAndClampNumericValue<long>(s, DxfTextReader.TryParseLongValue, long.MinValue, long.MaxValue);
            Assert.Equal(expected, actual);
        }
    }
}
