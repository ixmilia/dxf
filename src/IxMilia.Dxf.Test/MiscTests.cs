using System.Linq;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
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

        [Fact]
        public void GetInsertEntitiesWhenAddedToFile()
        {
            var line = new DxfLine(new DxfPoint(0.0, 0.0, 0.0), new DxfPoint(1.0, 1.0, 0.0));

            var block = new DxfBlock();
            block.Name = "some-block";
            block.Entities.Add(line);

            var insert = new DxfInsert();
            insert.Name = "some-block";

            var file = new DxfFile();
            file.Blocks.Add(block);

            // no entities because it's not yet part of the file
            Assert.Null(insert.Entities);

            file.Entities.Add(insert);

            // and now that it's in the file the entities can be found
            var foundEntity = insert.Entities.Single();
            Assert.Same(line, foundEntity);
        }

        [Fact]
        public void InsertWithNonMatchingNameReturnsNoEntities()
        {
            // similar to the above test except that the entity is added to the block _after_ the name binding
            var block = new DxfBlock();
            block.Name = "some-block";

            var insert = new DxfInsert();
            insert.Name = "some-other-block";

            var file = new DxfFile();
            file.Blocks.Add(block);
            file.Entities.Add(insert);

            // no entities because the block names differ
            Assert.Null(insert.Entities);
        }
    }
}
