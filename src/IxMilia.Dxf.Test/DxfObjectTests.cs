// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using IxMilia.Dxf.Objects;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfObjectTests : AbstractDxfTests
    {
        private DxfObject GenObject(string typeString, string contents)
        {
            return Section("OBJECTS", $@"
  0
{typeString}
{contents.Trim()}
").Objects.Single();
        }

        [Fact]
        public void ReadSimpleObjectTest()
        {
            var proxyObject = GenObject("ACAD_PROXY_OBJECT", "");
            Assert.IsType<DxfAcadProxyObject>(proxyObject);
        }

        [Fact]
        public void WriteSimpleObjectTest()
        {
            var file = new DxfFile();
            file.Objects.Add(new DxfAcadProxyObject());
            VerifyFileContains(file, @"
  0
ACAD_PROXY_OBJECT
  5
A
100
AcDbProxyObject
 90
499
");
        }

        [Fact]
        public void ReadDataTableTest()
        {
            var table = (DxfDataTable)GenObject("DATATABLE", @"
 90
2
 91
2
  1
table-name
 92
10
  2
column-of-points
 10
1
 20
2
 30
3
 10
4
 20
5
 30
6
 92
3
  2
column-of-strings
  3
string 1
  3
string 2
");
            Assert.Equal(2, table.ColumnCount);
            Assert.Equal(2, table.RowCount);
            Assert.Equal("table-name", table.Name);

            Assert.Equal("column-of-points", table.ColumnNames[0]);
            Assert.Equal(new DxfPoint(1, 2, 3), (DxfPoint)table[0, 0]);
            Assert.Equal(new DxfPoint(4, 5, 6), (DxfPoint)table[1, 0]);

            Assert.Equal("column-of-strings", table.ColumnNames[1]);
            Assert.Equal("string 1", (string)table[0, 1]);
            Assert.Equal("string 2", (string)table[1, 1]);
        }

        [Fact]
        public void WriteDataTableTest()
        {
            var table = new DxfDataTable();
            table.Name = "table-name";
            table.SetSize(2, 2);
            table.ColumnNames.Add("column-of-points");
            table.ColumnNames.Add("column-of-strings");
            table[0, 0] = new DxfPoint(1, 2, 3);
            table[1, 0] = new DxfPoint(4, 5, 6);
            table[0, 1] = "string 1";
            table[1, 1] = "string 2";
            var file = new DxfFile();
            file.Objects.Add(table);
            VerifyFileContains(file, @"
  0
DATATABLE
  5
A
100
AcDbDataTable
 70
0
 90
2
 91
2
  1
table-name
 92
10
  2
column-of-points
 10
1.0
 20
2.0
 30
3.0
 10
4.0
 20
5.0
 30
6.0
 92
3
  2
column-of-strings
  3
string 1
  3
string 2
");
        }

        [Fact]
        public void ReadDictionaryTest()
        {
            var dict = (DxfDictionary)GenObject("DICTIONARY", @"
100
AcDbDictionary
280
1
281
1
  3
name-1
350
1
  3
name-2
350
2
  3
name-3
350
3
");
            Assert.True(dict.IsHardOwner);
            Assert.Equal(DxfDictionaryDuplicateRecordHandling.KeepExisting, dict.DuplicateRecordHandling);
            Assert.Equal(3, dict.Count);
            Assert.Equal(1u, dict["name-1"]);
            Assert.Equal(2u, dict["name-2"]);
            Assert.Equal(3u, dict["name-3"]);
        }

        [Fact]
        public void WriteDictionaryTest()
        {
            var dict = new DxfDictionary();
            dict["name-1"] = 1u;
            dict["name-2"] = 2u;
            dict["name-3"] = 3u;
            dict.IsHardOwner = true;
            dict.DuplicateRecordHandling = DxfDictionaryDuplicateRecordHandling.KeepExisting;
            var file = new DxfFile();
            file.Objects.Add(dict);
            VerifyFileContains(file, @"
  0
DICTIONARY
  5
A
100
AcDbDictionary
280
1
281
1
  3
name-1
350
1
  3
name-2
350
2
  3
name-3
350
3
");
        }
    }
}
