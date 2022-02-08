using System.Linq;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Objects;
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

        [Fact]
        public void PointerResolutionInAnImageInABlockWhenReading()
        {
            var drawing = Parse(
                // blocks
                (0, "SECTION"),
                (2, "BLOCKS"),
                (0, "BLOCK"),
                (0, "IMAGE"),
                (340, "FFFF0340"), // points to the IMAGEDEF below
                (360, "FFFF0360"), // points to the IMAGEDEF_REACTOR below
                (0, "ENDSEC"),
                // objects
                (0, "SECTION"),
                (2, "OBJECTS"),
                (0, "IMAGEDEF"),
                (5, "FFFF0340"), // from IMAGE.340 above
                (1, "image-def-file-path"),
                (0, "IMAGEDEF_REACTOR"),
                (5, "FFFF0360"), // from IMAGE.360 above
                (0, "ENDSEC"),
                (0, "EOF")
            );
            var block = drawing.Blocks.Single();
            var imageEntity = (DxfImage)block.Entities.Single();
            var imageDef = imageEntity.ImageDefinition;
            Assert.Equal("image-def-file-path", imageDef.FilePath);
        }

        [Fact]
        public void PointerAssignmentInAnImageInABlockWhenWriting()
        {
            var file = new DxfFile();
            var block = new DxfBlock();
            block.Name = "my-block";
            var image = new DxfImage();
            image.ImageDefinition = new DxfImageDefinition();
            image.ImageDefinition.FilePath = "image-def-file-path";
            block.Entities.Add(image);
            file.Blocks.Add(block);

            // don't care about the code pairs, but this forces handles to be assigned to pointers
            var _actualCodePairs = file.GetCodePairs().ToList();

            Assert.NotEqual(0u, ((IDxfItemInternal)block).Handle.Value);
            Assert.NotEqual(0u, ((IDxfItemInternal)image.ImageDefinition).Handle.Value);
        }
    }
}
