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
    }
}
