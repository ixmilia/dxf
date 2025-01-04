using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using Xunit;

namespace Examples
{
    public class TestExamples
    {
        [Fact]
        public void InsertBlockR12()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R12; // this example has only been tested on R12 drawings

            // create a block with a line from (0,0) to (1,1)
            var block = new DxfBlock();
            block.Name = "my-block";
            block.Entities.Add(new DxfLine(new DxfPoint(0.0, 0.0, 0.0), new DxfPoint(1.0, 1.0, 0.0)));
            file.Blocks.Add(block);

            // insert a copy of the block at location (3,3); the result is a line from (3,3) to (4,4)
            var insert = new DxfInsert();
            insert.Name = "my-block"; // this is the name of the block that we're going to insert
            insert.Location = new DxfPoint(3.0, 3.0, 0.0);
            file.Entities.Add(insert);

            file.SaveExample();
        }

        [Fact]
        public void AddRadiusDimensionForACircle()
        {
            // this test has only been verified against LibreCAD 2.1.3
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R12;

            // add a circle at (0,0) with radius 1
            var circle = new DxfCircle(new DxfPoint(0.0, 0.0, 0.0), 1.0);
            file.Entities.Add(circle);

            // add a radius dimension that corresponds to the circle
            var dim = new DxfRadialDimension();
            dim.DefinitionPoint1 = circle.Center;
            dim.DefinitionPoint2 = circle.Center + new DxfVector(circle.Radius, 0.0, 0.0);
            file.Entities.Add(dim);

            file.SaveExample();
        }

        [Fact]
        public void AddACustomLineType()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R12;

            // create a custom line type and add it to the file
            // this line type will have a dash of length 0.5 followed by a gap of length 0.25
            var dashed = new DxfLineType("dashed");
            dashed.Elements.Add(new DxfLineTypeElement() { DashDotSpaceLength = 0.5 });
            dashed.Elements.Add(new DxfLineTypeElement() { DashDotSpaceLength = 0.25 });
            file.LineTypes.Add(dashed);

            // now add a line using this line type
            var line = new DxfLine(new DxfPoint(0.0, 0.0, 0.0), new DxfPoint(10.0, 10.0, 0.0));
            line.LineTypeName = dashed.Name;
            file.Entities.Add(line);

            file.SaveExample();
        }

        [Fact]
        public void SpecifyFontForTextEntity()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R14;

            // add a text style with a specific font
            var textStyle = new DxfStyle("MY_TEXT_STYLE"); // this name is what will assign the font to TEXT entities
            textStyle.PrimaryFontFileName = "Times New Roman";
            file.Styles.Add(textStyle);

            // add a text entity with the appropriate style name
            var text = new DxfText(new DxfPoint(10, 10, 0), 2.0, "This is text.");
            text.TextStyleName = textStyle.Name; // either assign the name from the text style, or you can maually specify "MY_TEXT_STYLE" from above
            file.Entities.Add(text);

            file.SaveExample();
        }
    }
}
