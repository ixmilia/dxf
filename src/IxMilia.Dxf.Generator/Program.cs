// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf.Generator
{
    public class Program
    {
        private const string EntityDirString = "--entityDir=";
        private const string ObjectDirString = "--objectDir=";

        public static void Main(string[] args)
        {
            string entityDir = "Entities";
            string objectDir = "Objects";
            foreach (var arg in args)
            {
                if (arg.StartsWith(EntityDirString))
                {
                    entityDir = arg.Substring(EntityDirString.Length);
                }
                else if (arg.StartsWith(ObjectDirString))
                {
                    objectDir = arg.Substring(ObjectDirString.Length);
                }
            }

            Console.WriteLine($"Generating entities into: {entityDir}");
            Console.WriteLine($"Generating objects into: {objectDir}");
            new EntityGenerator(entityDir).Run();
            new ObjectGenerator(objectDir).Run();
        }
    }
}
