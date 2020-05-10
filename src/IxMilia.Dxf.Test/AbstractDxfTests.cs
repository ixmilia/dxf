// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using IxMilia.Dxf.Sections;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public abstract class AbstractDxfTests
    {
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

        internal static string ToString(DxfFile file)
        {
            using (var stream = new MemoryStream())
            {
                file.Save(stream);
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        internal static string ToString(DxfFile file, DxfSectionType sectionType)
        {
            using (var stream = new MemoryStream())
            {
                file.WriteSingleSection(stream, sectionType);
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        internal static void VerifyFileContents(DxfFile file, string expected, DxfSectionType? sectionType, Action<string, string> predicate)
        {
            var actual = sectionType.HasValue
                ? ToString(file, sectionType.GetValueOrDefault())
                : ToString(file);
            predicate(
                RemoveLeadingAndTrailingWhitespaceFromLines(expected),
                RemoveLeadingAndTrailingWhitespaceFromLines(actual));
        }

        private static string RemoveLeadingAndTrailingWhitespaceFromLines(string s)
        {
            var lines = s.Split("\n".ToCharArray()).Select(l => l.Trim());
            return string.Join("\r\n", lines);
        }

        protected static string FixNewLines(string s)
        {
            return s.Replace("\r", "").Replace("\n", "\r\n");
        }

        internal static void VerifyFileContains(DxfFile file, string expected, DxfSectionType? sectionType = null)
        {
            VerifyFileContents(file, expected, sectionType, (ex, ac) => AssertRegexContains(ex.Trim(), ac));
        }

        internal static void VerifyFileDoesNotContain(DxfFile file, string unexpected, DxfSectionType? sectionType = null)
        {
            VerifyFileContents(file, unexpected, sectionType, predicate: (ex, ac) => Assert.DoesNotContain(ex.Trim(), ac));
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

        private static void AssertRegexContains(string expected, string actual)
        {
            var regex = CreateMatcherRegex(expected);
            if (!regex.IsMatch(actual))
            {
                throw new Exception(string.Join(Environment.NewLine, new[]
                    {
                        "Unable to find",
                        expected,
                        "in",
                        actual
                    }));
            }
        }

        private static Regex CreateMatcherRegex(string text)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                switch (c)
                {
                    case '#':
                        // the character '#' will match any hex number (item handle)
                        sb.Append(@"[A-Fa-f0-9]+");
                        break;
                    case '.':
                    case '\\':
                    case '[':
                    case ']':
                    case '(':
                    case ')':
                    case '?':
                    case '*':
                    case '+':
                    case '$':
                    case '^':
                        // escape special characters
                        sb.Append("\\" + c);
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return new Regex(sb.ToString(), RegexOptions.Multiline);
        }
    }
}
