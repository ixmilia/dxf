using System.Linq;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class BlockTests : AbstractDxfTests
    {
        // test is inspired by this page: https://ezdxf.readthedocs.io/en/stable/dxfinternals/block_management.html
        [Fact]
        public void FileLayoutWithBlockSavedAsR12()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R12;

            var block = new DxfBlock();
            block.Name = "my-block";
            block.Entities.Add(new DxfLine(new DxfPoint(0.0, 0.0, 0.0), new DxfPoint(1.0, 1.0, 0.0)));
            file.Blocks.Add(block);

            var insert = new DxfInsert();
            insert.Name = "my-block";
            insert.Location = new DxfPoint(3.0, 3.0, 0.0);
            file.Entities.Add(insert);

            var actualCodePairs = file.GetCodePairs().ToList();
            AssertContains(actualCodePairs,
                (0, "BLOCK"),
                // no handle
                (8, "0"),
                (2, "my-block"),
                (70, 0),
                (10, 0.0),
                (20, 0.0),
                (30, 0.0),
                (3, "my-block"),
                (1, ""),
                (0, "LINE"),
                (5, "#")
            );
            AssertContains(actualCodePairs,
                (0, "ENDBLK"),
                (5, "#"), // only endblk gets a handle
                (8, "0")
            );
            AssertContains(actualCodePairs,
                (0, "INSERT"),
                (5, "#"),
                (8, "0"),
                (2, "my-block"),
                (10, 3.0),
                (20, 3.0),
                (30, 0.0),
                (0, "ENDSEC") // ensures nothing else was written
            );
        }
    }
}
