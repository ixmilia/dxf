using System;
using System.IO;

namespace IxMilia.Dxf.Generator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: requires one argument specifying the location of the root project.");
                Environment.Exit(1);
            }

            var projectDir = args[0];
            var entityDir = Path.Combine(projectDir, "Entities");
            var objectDir = Path.Combine(projectDir, "Objects");
            var sectionDir = Path.Combine(projectDir, "Sections");
            var tableDir = Path.Combine(projectDir, "Tables");

            CleanDirectory(entityDir);
            CleanDirectory(objectDir);
            CleanDirectory(sectionDir);
            CleanDirectory(tableDir);

            Console.WriteLine($"Generating entities into: {entityDir}");
            Console.WriteLine($"Generating objects into: {objectDir}");
            Console.WriteLine($"Generating sections into: {sectionDir}");
            Console.WriteLine($"Generating tables into: {tableDir}");

            new EntityGenerator(entityDir).Run();
            new ObjectGenerator(objectDir).Run();
            new SectionGenerator(sectionDir).Run();
            new TableGenerator(tableDir).Run();
        }

        private static void CleanDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }

            Directory.CreateDirectory(directoryPath);
        }
    }
}
