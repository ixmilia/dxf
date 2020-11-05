using System.IO;
using System.Linq;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Objects;
using IxMilia.Dxf.Sections;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class XDataTests : AbstractDxfTests
    {
        [Fact]
        public void AssignOwnerHandlesInXDataTest()
        {
            // read a layout with its owner handle also specified in the XDATA
            var file = Parse(
                (0, "SECTION"),
                (2, "HEADER"),
                (9, "$ACADVER"),
                (1, "AC1027"),
                (0, "ENDSEC"),
                (0, "SECTION"),
                (2, "OBJECTS"),
                (0, "DICTIONARY"),
                (5, "BBBBBBBB"),
                (3, "some-layout"),
                (350, "CCCCCCCC"),
                (0, "LAYOUT"),
                (5, "CCCCCCCC"),
                (330, "BBBBBBBB"),
                (102, "{ACAD_REACTORS"),
                (330, "BBBBBBBB"),
                (102, "}"),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            // sanity check to verify that it was read correctly
            var dict = file.Objects.OfType<DxfDictionary>().Single();
            var layout = (DxfLayout)dict["some-layout"];
            Assert.Equal(0xBBBBBBBB, ((IDxfItemInternal)dict).Handle);
            Assert.Equal(0xCCCCCCCC, ((IDxfItemInternal)layout).Handle);

            // re-save the file to a garbage stream to re-assign handles
            using (var ms = new MemoryStream())
            {
                file.Save(ms);
            }

            // verify new handles and owners; note that the assigned handles are unlikely to be 0xBBBBBBBB and 0xCCCCCCCC again
            Assert.True(ReferenceEquals(layout.Owner, dict));
            Assert.NotEqual(0xBBBBBBBB, ((IDxfItemInternal)dict).Handle);
            Assert.NotEqual(0xCCCCCCCC, ((IDxfItemInternal)layout).Handle);
            var dictHandle = ((IDxfItemInternal)dict).Handle;
            Assert.Equal(dictHandle, ((IDxfItemInternal)layout).OwnerHandle);
            var layoutXDataGroups = ((IDxfHasXData)layout).ExtensionDataGroups.Single(g => g.GroupName == "ACAD_REACTORS");
            var ownerCodePair = (DxfCodePair)layoutXDataGroups.Items.Single();
            Assert.Equal(330, ownerCodePair.Code);
            Assert.Equal(DxfCommonConverters.UIntHandle(dictHandle), ownerCodePair.StringValue);
        }

        [Fact]
        public void ReadXDataFromCommonEntityTest()
        {
            var file = Section("ENTITIES",
                (0, "LINE"),
                (1001, "group name"),
                (1002, "{"),
                (1011, 1.0), // valid world point
                (1021, 2.0),
                (1031, 3.0),
                (1021, 222.0), // point missing x value; invalid and skipped
                (1031, 333.0),
                (1011, 11.0), // valid world point
                (1021, 22.0),
                (1031, 33.0),
                (1002, "}")
            );
            var line = (DxfLine)file.Entities.Single();
            var itemCollection = line.XData.Single().Value;
            var itemList = (DxfXDataItemList)itemCollection.Single();
            Assert.Equal(2, itemList.Items.Count);
            Assert.Equal(new DxfPoint(1, 2, 3), ((DxfXDataWorldSpacePosition)itemList.Items[0]).Value);
            Assert.Equal(new DxfPoint(11, 22, 33), ((DxfXDataWorldSpacePosition)itemList.Items[1]).Value);
        }

        [Fact]
        public void ReadMultipleXDataFromEntityTest()
        {
            var file = Section("ENTITIES",
                (0, "LINE"),
                (1001, "group_name_1"),
                (1040, 1.0),
                (1040, 2.0),
                (1001, "group_name_2"),
                (1002, "{"),
                (1011, 11.0),
                (1021, 22.0),
                (1031, 33.0),
                (1002, "}"),
                (1040, 3.0)
            );
            var line = (DxfLine)file.Entities.Single();
            Assert.Equal(2, line.XData.Count);

            var first = line.XData["group_name_1"];
            Assert.Equal(2, first.Count);
            Assert.Equal(1.0, ((DxfXDataReal)first[0]).Value);
            Assert.Equal(2.0, ((DxfXDataReal)first[1]).Value);

            var second = line.XData["group_name_2"];
            Assert.Equal(2, second.Count);
            var list = (DxfXDataItemList)second[0];
            Assert.Equal(new DxfPoint(11.0, 22.0, 33.0), ((DxfXDataWorldSpacePosition)list.Items.Single()).Value);
            Assert.Equal(3.0, ((DxfXDataReal)second[1]).Value);
        }

        [Fact]
        public void WriteMultipleXDataFromEntityTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            var line = new DxfLine();
            line.XData["group_name_1"] = new DxfXDataApplicationItemCollection(
                new DxfXDataReal(1.0),
                new DxfXDataReal(2.0)
            );
            line.XData["group_name_2"] = new DxfXDataApplicationItemCollection(
                new DxfXDataItemList(new[] { new DxfXDataWorldSpacePosition(new DxfPoint(11.0, 22.0, 33.0)) }),
                new DxfXDataReal(3.0)
            );
            file.Entities.Add(line);
            VerifyFileContains(file,
                DxfSectionType.Entities,
                (1001, "group_name_1"),
                (1040, 1.0),
                (1040, 2.0),
                (1001, "group_name_2"),
                (1002, "{"),
                (1011, 11.0),
                (1021, 22.0),
                (1031, 33.0),
                (1002, "}"),
                (1040, 3.0)
            );
        }

        [Fact]
        public void ReadDimStyleXDataTest()
        {
            var file = Parse(
                (0, "SECTION"),
                (2, "ENTITIES"),
                (0, "DIMENSION"),
                (100, "AcDbAlignedDimension"),
                (1001, "ACAD"),
                (1000, "leading string"),
                (1000, "DSTYLE"),
                (1002, "{"),
                (1070, 54),
                (1000, "some string"),
                (1002, "}"),
                (1070, 42),
                (0, "ENDSEC"),
                (0, "EOF")
            );
            var dim = (DxfAlignedDimension)file.Entities.Single();
            var xdataPair = dim.XData.Single();
            Assert.Equal("ACAD", xdataPair.Key);
            var xdataItems = xdataPair.Value;
            Assert.Equal(4, xdataItems.Count);

            Assert.Equal("leading string", ((DxfXDataString)xdataItems[0]).Value);

            Assert.Equal("DSTYLE", ((DxfXDataString)xdataItems[1]).Value);

            var list = (DxfXDataItemList)xdataItems[2];
            Assert.Equal(2, list.Items.Count);
            Assert.Equal(54, ((DxfXDataInteger)list.Items[0]).Value);
            Assert.Equal("some string", ((DxfXDataString)list.Items[1]).Value);

            Assert.Equal(42, ((DxfXDataInteger)xdataItems[3]).Value);
        }

        [Fact]
        public void WriteDimStyleXDataTest()
        {
            var dim = new DxfAlignedDimension();
            dim.XData.Add("ACAD",
                new DxfXDataApplicationItemCollection(
                    new DxfXDataString("DSTYLE"),
                    new DxfXDataItemList(
                        new DxfXDataInteger(271),
                        new DxfXDataInteger(9)
                    )
                ));
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;
            file.Entities.Add(dim);
            VerifyFileContains(file,
                DxfSectionType.Entities,
                (1001, "ACAD"),
                (1000, "DSTYLE"),
                (1002, "{"),
                (1070, 271),
                (1070, 9),
                (1002, "}")
            );
        }
    }
}
