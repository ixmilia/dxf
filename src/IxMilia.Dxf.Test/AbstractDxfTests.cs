// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public abstract class AbstractDxfTests
    {
        protected static DxfFile Parse(string data)
        {
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            {
                writer.WriteLine(data.Trim());
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                return DxfFile.Load(ms);
            }
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

        protected static string ToString(DxfFile file)
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

        protected static void VerifyFileContents(DxfFile file, string expected, Action<string, string> predicate)
        {
            var actual = ToString(file);
            predicate(expected, actual);
        }

        protected static void VerifyFileContains(DxfFile file, string expected)
        {
            VerifyFileContents(file, expected, (ex, ac) => Assert.Contains(ex.Trim(), ac));
        }

        protected static void VerifyFileDoesNotContain(DxfFile file, string unexpected)
        {
            VerifyFileContents(file, unexpected, (ex, ac) => Assert.DoesNotContain(ex.Trim(), ac));
        }

        protected static void VerifyFileIsExactly(DxfFile file, string expected)
        {
            VerifyFileContents(file, expected, (ex, ac) => Assert.Equal(ex.Trim(), ac.Trim()));
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
            return type.IsGenericType && type.GenericTypeArguments.Length == 1 && type.Name == "List`1";
        }

        protected static T SetAllPropertiesToDefault<T>(T item)
        {
            foreach (var property in item.GetType().GetProperties().Where(p => p.GetSetMethod() != null && p.GetIndexParameters().Length == 0))
            {
                var propertyType = property.PropertyType;
                var defaultValue = propertyType.IsValueType
                    ? Activator.CreateInstance(propertyType)
                    : null;
                property.SetValue(item, defaultValue);
            }

            return item;
        }
    }
}
