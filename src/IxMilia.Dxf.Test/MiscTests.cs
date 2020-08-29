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

            var items = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, modified);
            Assert.Equal("DSTYLE", ((DxfXDataString)items.First()).Value);
        }

        [Fact]
        public void DimStyleDifferenceXDataOnSinglePropertyDifference()
        {
            var primary = new DxfDimStyle();
            var modified = new DxfDimStyle()
            {
                DimensionUnitToleranceDecimalPlaces = (short)(primary.DimensionUnitToleranceDecimalPlaces + 1)
            };

            var diffItems = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, modified);
            Assert.Equal(2, diffItems.Count);

            Assert.Equal("DSTYLE", ((DxfXDataString)diffItems[0]).Value);

            var list = (DxfXDataItemList)diffItems[1];
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

            var diffItems = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, modified);
            Assert.Equal(2, diffItems.Count);

            Assert.Equal("DSTYLE", ((DxfXDataString)diffItems[0]).Value);

            var list = (DxfXDataItemList)diffItems[1];
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

            var diffItems = DxfDimStyle.GenerateStyleDifferenceAsXData(primary, secondary);

            Assert.Equal(2, diffItems.Count);

            Assert.Equal("DSTYLE", ((DxfXDataString)diffItems[0]).Value);

            var list = (DxfXDataItemList)diffItems[1];
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

        [Fact]
        public void DimStyleGetVariable()
        {
            var style = new DxfDimStyle()
            {
                DimensionUnitToleranceDecimalPlaces = 5
            };
            Assert.Equal((short)5, style.GetVariable("DIMDEC"));
        }

        [Fact]
        public void DimStyleSetVariable()
        {
            var style = new DxfDimStyle();
            Assert.NotEqual(5, style.DimensionUnitToleranceDecimalPlaces);
            style.SetVariable("DIMDEC", (short)5);
            Assert.Equal(5, style.DimensionUnitToleranceDecimalPlaces);
        }
    }
}
