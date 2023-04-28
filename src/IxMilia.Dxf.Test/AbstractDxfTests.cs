using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Sections;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public abstract class AbstractDxfTests
    {
        protected DateTime RoundToNearestSecond(DateTime dateTime)
        {
            var seconds = dateTime.Second + ((dateTime.Millisecond + 500.0) / 1000.0);
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, (int)seconds, dateTime.Kind);
        }

        protected static DxfFile Parse(string data)
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, new UTF8Encoding(false)))
            {
                writer.WriteLine(data.Trim());
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                return DxfFile.Load(ms);
            }
        }

        protected static DxfFile Parse(params (int code, object value)[] codePairs)
        {
            var testBuffer = new TestCodePairBufferReader(codePairs);
            var file = DxfFile.LoadFromReader(testBuffer);
            return file;
        }

        protected static DxfFile Section(string sectionName, string data)
        {
            return Parse(string.Format(@"
0
SECTION
2
{0}{1}
0
ENDSEC
0
EOF
", sectionName, string.IsNullOrWhiteSpace(data) ? null : "\r\n" + data.Trim()));
        }

        protected static DxfFile Section(string sectionName, params (int code, object value)[] codePairs)
        {
            return Section(sectionName, (IEnumerable<(int code, object value)>)codePairs);
        }

        protected static DxfFile Section(string sectionName, IEnumerable<(int code, object value)> codePairs)
        {
            var preCodePairs = new[]
            {
                (0, (object)"SECTION"),
                (2, sectionName),
            };
            var postCodePairs = new[]
            {
                (0, (object)"ENDSEC"),
                (0, "EOF"),
            };
            var testBuffer = new TestCodePairBufferReader(preCodePairs.Concat(codePairs).Concat(postCodePairs));
            var file = DxfFile.LoadFromReader(testBuffer);
            return file;
        }

        internal static DxfFile RoundTrip(DxfFile file)
        {
            var pairs = file.GetCodePairs();
            var roundTrippedFile = DxfFile.LoadFromReader(new TestCodePairBufferReader(pairs));
            return roundTrippedFile;
        }

        internal static DxfTextReader TextReaderFromLines(Encoding defaultTextEncoding, params string[] lines)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            for (int i = 1; i < lines.Length; i++)
            {
                writer.WriteLine(lines[i]);
            }

            writer.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            var firstLine = lines[0];
            return new DxfTextReader(ms, defaultTextEncoding, firstLine);
        }

        internal static DxfTextReader TextReaderFromLines(params string[] lines)
        {
            return TextReaderFromLines(Encoding.UTF8, lines);
        }

        internal static byte[] WriteToBinaryWriter(params (int code, object value)[] pairs)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new DxfWriter(ms, asText: false, version: DxfAcadVersion.R2004);
                writer.CreateInternalWriters();
                foreach (var pair in pairs)
                {
                    var properPair = new DxfCodePair(pair.code, pair.value);
                    writer.WriteCodeValuePair(properPair);
                }

                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }

        internal static string WriteToTextWriter(params (int code, object value)[] pairs)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new DxfWriter(ms, asText: true, version: DxfAcadVersion.R2004);
                writer.CreateInternalWriters();
                foreach (var pair in pairs)
                {
                    var properPair = new DxfCodePair(pair.code, pair.value);
                    writer.WriteCodeValuePair(properPair);
                }

                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        internal static bool AreCodePairsEquivalent(DxfCodePair a, DxfCodePair b)
        {
            if (a.Code == b.Code &&
                DxfCodePair.ExpectedType(a.Code) == typeof(string) &&
                (a.StringValue == "#" || b.StringValue == "#"))
            {
                // fake handle placeholder
                return true;
            }

            return a == b;
        }

        internal static int IndexOf(IEnumerable<DxfCodePair> superset, params (int code, object value)[] subset)
        {
            return IndexOf(superset.ToList(), subset.Select(cp => new DxfCodePair(cp.code, cp.value)).ToList());
        }

        internal static int IndexOf(List<DxfCodePair> superset, List<DxfCodePair> subset)
        {
            var upperBound = superset.Count - subset.Count;
            for (int supersetIndex = 0; supersetIndex <= upperBound; supersetIndex++)
            {
                bool isMatch = true;
                for (int subsetIndex = 0; subsetIndex < subset.Count && isMatch; subsetIndex++)
                {
                    var expected = subset[subsetIndex];
                    var actual = superset[supersetIndex + subsetIndex];
                    isMatch &= AreCodePairsEquivalent(expected, actual);
                }

                if (isMatch)
                {
                    // found it
                    return supersetIndex;
                }
            }

            return -1;
        }

        internal static void VerifyFileContains(DxfFile file, params (int code, object value)[] codePairs)
        {
            var expectedPairs = codePairs.Select(cp => new DxfCodePair(cp.code, cp.value)).ToList();
            var actualPairs = file.GetCodePairs().ToList();
            var index = IndexOf(actualPairs, expectedPairs);
            if (index < 0)
            {
                var expectedString = string.Join("\n", expectedPairs);
                var actualString = string.Join("\n", actualPairs);

                throw new Exception($"Unable to find expected pairs\n{expectedString}\n\nin\n\n{actualString}.");
            }
        }

        internal static void VerifyFileContains(DxfFile file, DxfSectionType sectionType, params (int code, object value)[] codePairs)
        {
            var expectedPairs = codePairs.Select(cp => new DxfCodePair(cp.code, cp.value)).ToList();
            var sections = file.Sections.Where(s => s.Type == sectionType);
            var actualPairs = file.GetCodePairs(sections).ToList();
            var index = IndexOf(actualPairs, expectedPairs);
            if (index < 0)
            {
                var expectedString = string.Join("\n", expectedPairs);
                var actualString = string.Join("\n", actualPairs);

                throw new Exception($"Unable to find expected pairs\n{expectedString}\n\nin\n\n{actualString}.");
            }
        }

        internal static void VerifyFileDoesNotContain(DxfFile file, params (int code, object value)[] codePairs)
        {
            var expectedPairs = codePairs.Select(cp => new DxfCodePair(cp.code, cp.value)).ToList();
            var actualPairs = file.GetCodePairs().ToList();
            var index = IndexOf(actualPairs, expectedPairs);
            if (index >= 0)
            {
                var expectedString = string.Join("\n", expectedPairs);
                var actualString = string.Join("\n", actualPairs);

                throw new Exception($"Expected not to find\n{expectedString}\n\nin\n\n{actualString}.\n\nItem was found at index {index}.");
            }
        }

        internal static void VerifyFileDoesNotContain(DxfFile file, DxfSectionType sectionType, params (int code, object value)[] codePairs)
        {
            var expectedPairs = codePairs.Select(cp => new DxfCodePair(cp.code, cp.value)).ToList();
            var sections = file.Sections.Where(s => s.Type == sectionType);
            var actualPairs = file.GetCodePairs(sections).ToList();
            var index = IndexOf(actualPairs, expectedPairs);
            if (index >= 0)
            {
                var expectedString = string.Join("\n", expectedPairs);
                var actualString = string.Join("\n", actualPairs);

                throw new Exception($"Expected not to find\n{expectedString}\n\nin\n\n{actualString}.\n\nItem was found at index {index}.");
            }
        }

        protected static void AssertArrayEqual<T>(T[] expected, T[] actual)
        {
            if (expected == null)
            {
                Assert.Null(actual);
            }

            Assert.NotNull(actual);
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        protected static bool IsListOfT(Type type)
        {
            return type.GetTypeInfo().IsGenericTypeDefinition && type.GenericTypeArguments.Length == 1 && type.Name == "List`1";
        }

        protected static T SetAllPropertiesToDefault<T>(T item)
        {
            foreach (var property in item.GetType().GetTypeInfo().GetProperties().Where(p => p.GetSetMethod() != null && p.GetIndexParameters().Length == 0))
            {
                var propertyType = property.PropertyType;
                var defaultValue = propertyType.GetTypeInfo().IsValueType
                    ? Activator.CreateInstance(propertyType)
                    : null;
                property.SetValue(item, defaultValue);
            }

            return item;
        }

        protected static void AssertEquivalent<T>(T expected, T actual)
        {
            foreach (var field in typeof(T).GetFields())
            {
                var expectedField = field.GetValue(expected);
                var actualField = field.GetValue(actual);
                Assert.Equal(expectedField, actualField);
            }

            foreach (var property in typeof(T).GetProperties())
            {
                var expectedProperty = property.GetValue(expected);
                var actualProperty = property.GetValue(actual);
                Assert.Equal(expectedProperty, actualProperty);
            }
        }

        protected static void AssertContains(IEnumerable<DxfCodePair> actualPairs, params (int code, object value)[] expectedPairs)
        {
            var index = IndexOf(actualPairs, expectedPairs);
            if (index < 0)
            {
                var expectedString = string.Join("\n", expectedPairs);
                var actualString = string.Join("\n", actualPairs);

                throw new Exception($"Unable to find expected pairs\n{expectedString}\n\nin\n\n{actualString}.");
            }
        }

        public static IEnumerable<Type> GetAllEntityTypes()
        {
            var assembly = typeof(DxfFile).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (ReaderWriterTests.IsEntityOrDerived(type) && !type.IsAbstract && type != typeof(DxfDimensionBase))
                {
                    yield return type;
                }
            }
        }

        public static IEnumerable<Type> GetAllObjectTypes()
        {
            var assembly = typeof(DxfFile).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (ReaderWriterTests.IsObjectOrDerived(type) && !type.IsAbstract)
                {
                    yield return type;
                }
            }
        }
    }
}
