using System.IO;
using System.Runtime.CompilerServices;
using IxMilia.Dxf;

namespace Examples
{
    internal static class TestExtensions
    {
        internal static void SaveExample(this DxfFile file, [CallerFilePath] string testFilePath = null, [CallerMemberName] string testName = null)
        {
            var testDirectory = Path.GetDirectoryName(testFilePath);
            var fullTestDirectory = Path.Combine(testDirectory, "SavedExamples");
            Directory.CreateDirectory(fullTestDirectory);

            var fileName = $"{testName}.dxf";
            var fullOutputPath = Path.Combine(fullTestDirectory, fileName);
            file.Save(fullOutputPath);
        }
    }
}
