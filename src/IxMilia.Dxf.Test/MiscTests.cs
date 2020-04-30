// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class MiscTests : AbstractDxfTests
    {
        [Fact]
        public void NoDimStyleDifferenceGeneratesNullXData()
        {
            var primary = new DxfDimStyle();
            var modified = new DxfDimStyle();
            var xdata = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, modified);
            Assert.Null(xdata);
        }

        [Fact]
        public void DimStyleDifferenceXDataHasWellKnownName()
        {
            var primary = new DxfDimStyle();
            var modified = new DxfDimStyle()
            {
                DimensionUnitToleranceDecimalPlaces = (short)(primary.DimensionUnitToleranceDecimalPlaces + 1)
            };

            var xdata = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, modified);
            var list = (DxfXDataNamedList)xdata.Items.Single();
            Assert.Equal("DSTYLE", list.Name);
        }

        [Fact]
        public void DimStyleDifferenceXDataOnSinglePropertyDifference()
        {
            var primary = new DxfDimStyle();
            var modified = new DxfDimStyle()
            {
                DimensionUnitToleranceDecimalPlaces = (short)(primary.DimensionUnitToleranceDecimalPlaces + 1)
            };

            var xdata = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, modified);
            var list = (DxfXDataNamedList)xdata.Items.Single();
            Assert.Equal(2, list.Items.Count);

            Assert.Equal(271, ((DxfXDataInteger)list.Items[0]).Value);
            Assert.Equal(modified.DimensionUnitToleranceDecimalPlaces, ((DxfXDataInteger)list.Items[1]).Value);
        }

        [Fact]
        public void DimStyleDifferenceXDataOnMultiplePropertyDifferences()
        {
            var primary = new DxfDimStyle();
            var modified = new DxfDimStyle()
            {
                DimensioningSuffix = "non-standard-suffix",
                DimensionUnitToleranceDecimalPlaces = (short)(primary.DimensionUnitToleranceDecimalPlaces + 1)
            };

            var xdata = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, modified);
            var list = (DxfXDataNamedList)xdata.Items.Single();
            Assert.Equal(4, list.Items.Count);

            Assert.Equal(3, ((DxfXDataInteger)list.Items[0]).Value);
            Assert.Equal("non-standard-suffix", ((DxfXDataString)list.Items[1]).Value);

            Assert.Equal(271, ((DxfXDataInteger)list.Items[2]).Value);
            Assert.Equal(modified.DimensionUnitToleranceDecimalPlaces, ((DxfXDataInteger)list.Items[3]).Value);
        }

        [Fact]
        public void DimStyleDiffernceAfterClone()
        {
            var primary = new DxfDimStyle();
            var secondary = primary.Clone();
            secondary.DimensionUnitToleranceDecimalPlaces = 5;

            var xdata = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, secondary);
            var list = (DxfXDataNamedList)xdata.Items.Single();

            Assert.Equal(2, list.Items.Count);

            Assert.Equal(271, ((DxfXDataInteger)list.Items[0]).Value);
            Assert.Equal(5, ((DxfXDataInteger)list.Items[1]).Value);
        }

        [Fact]
        public void DimStyleFromCustomXData()
        {
            var primary = new DxfDimStyle();
            var secondary = new DxfDimStyle()
            {
                DimensionUnitToleranceDecimalPlaces = 5
            };

            // sanity check that the values are different
            Assert.NotEqual(primary.DimensionUnitToleranceDecimalPlaces, secondary.DimensionUnitToleranceDecimalPlaces);

            // rebuild dim style from primary with xdata difference; result should equal secondary
            var xdata = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, secondary);
            Assert.True(primary.TryGetStyleFromXDataDifference(xdata, out var reBuiltStyle));
            AssertEquivalent(secondary, reBuiltStyle);
        }
    }
}
