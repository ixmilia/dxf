// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfFileTests : AbstractDxfTests
    {
        [Fact]
        public void EmptyFileBoundingBoxTest()
        {
            Assert.Equal(new DxfBoundingBox(DxfPoint.Origin, DxfVector.Zero), new DxfFile().GetBoundingBox());
        }

        [Fact]
        public void FileBoundingBoxTest()
        {
            var file = new DxfFile();
            var line = new DxfLine(new DxfPoint(0.0, 1.0, 0.0), new DxfPoint(1.0, 0.0, 0.0));
            file.Entities.Add(line);
            Assert.Equal(new DxfBoundingBox(DxfPoint.Origin, new DxfVector(1.0, 1.0, 0.0)), file.GetBoundingBox());
        }
    }
}
