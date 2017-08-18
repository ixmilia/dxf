// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace IxMilia.Dxf.Generator
{
    public class Program
    {
        private const string GeneratedString = "Generated";

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: requires one argument specifying the location of the root project.");
                Environment.Exit(1);
            }

            var projectDir = args[0];
            var entityDir = Path.Combine(projectDir, "Entities", GeneratedString);
            var objectDir = Path.Combine(projectDir, "Objects", GeneratedString);
            var sectionDir = Path.Combine(projectDir, "Sections", GeneratedString);
            var tableDir = Path.Combine(projectDir, "Tables", GeneratedString);
            Console.WriteLine($"Generating entities into: {entityDir}");
            Console.WriteLine($"Generating objects into: {objectDir}");
            Console.WriteLine($"Generating sections into: {sectionDir}");
            Console.WriteLine($"Generating tables into: {tableDir}");
            new EntityGenerator(entityDir).Run();
            new ObjectGenerator(objectDir).Run();
            new SectionGenerator(sectionDir).Run();
            new TableGenerator(tableDir).Run();
        }
    }
}
