// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf.Generator
{
    public class Program
    {
        private const string EntityDirString = "--entityDir=";

        public static void Main(string[] args)
        {
            string entityDir = "Entities";
            foreach (var arg in args)
            {
                if (arg.StartsWith(EntityDirString))
                {
                    entityDir = arg.Substring(EntityDirString.Length);
                }
            }

            Console.WriteLine($"Generating entities into: {entityDir}");
            var generator = new EntityGenerator(entityDir);
            generator.Run();
        }
    }
}
