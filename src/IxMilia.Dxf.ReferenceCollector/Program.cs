// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace IxMilia.Dxf.ReferenceCollector
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Required arguments: The DXF spec version to download, e.g., '2018'.");
                Environment.Exit(1);
            }

            var dxfVersion = args[0];
            var linkVirtualRoot = $"http://help.autodesk.com/cloudhelp/{dxfVersion}/ENU/AutoCAD-DXF/"; // don't crawl above this path
            var startPageUrl = linkVirtualRoot + "files/index.htm";
            var resultFile = Path.Combine(Environment.CurrentDirectory, $"dxf-reference-R{dxfVersion}.html");
            var collector = new WebPageCollector(startPageUrl, linkVirtualRoot, resultFile);
            collector.Run();
        }
    }
}
