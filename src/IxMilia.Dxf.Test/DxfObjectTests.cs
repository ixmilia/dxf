// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using IxMilia.Dxf.Objects;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfObjectTests : AbstractDxfTests
    {
        [Fact]
        public void ReadSimpleObjectTest()
        {
            var file = Section("OBJECTS", @"
  0
ACAD_PROXY_OBJECT
");
            Assert.IsType<DxfAcadProxyObject>(file.Objects.Single());
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
    }
}
