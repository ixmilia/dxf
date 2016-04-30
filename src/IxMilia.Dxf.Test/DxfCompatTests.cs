// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfCompatTests : AbstractDxfTests
    {
        private static readonly string[] TeighaVersions = new[] { "ACAD9", "ACAD10", "ACAD12", "ACAD13", "ACAD14", "ACAD2000", "ACAD2004", "ACAD2007", "ACAD2010", "ACAD2013" };
        private static readonly string MinimumFileText = @"
  0
SECTION
  2
ENTITIES
  0
LINE
  8
0
 10
0.0
 20
0.0
 30
0.0
 11
10.0
 21
10.0
 31
0.0
  0
ENDSEC
  0
EOF
".Trim();

        private string PrepareTempDirectory(string dirName)
        {
            var tempDir = Path.GetTempPath();
            var fullPath = Path.Combine(tempDir, dirName);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }

            Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        [TeighaConverterExistsFact]
        public void IxMiliaReadTeighaTest()
        {
            // use Teigha to convert a minimum-working-file to each of its supported versions and try to open with IxMilia
            var exceptions = new List<Exception>();
            foreach (var teighaVersion in TeighaVersions)
            {
                var inputDir = PrepareTempDirectory("TeighaCompatInputDir");
                var outputDir = PrepareTempDirectory("TeighaCompatOutputDir");
                var barePath = Path.Combine(inputDir, "bare.dxf");
                File.WriteAllText(barePath, MinimumFileText);
                var psi = new ProcessStartInfo();
                psi.FileName = TeighaConverterExistsFactAttribute.GetPathToFileConverter();
                psi.Arguments = $@"""{inputDir}"" ""{outputDir}"" ""{teighaVersion}"" ""DXF"" ""0"" ""1""";
                //                                                                            recurse audit
                var proc = Process.Start(psi);
                proc.WaitForExit();
                Assert.Equal(0, proc.ExitCode);
                Assert.Equal(0, Directory.EnumerateFiles(outputDir, "*.err").Count());

                var convertedFilePath = Directory.EnumerateFiles(outputDir, "*.dxf").Single();
                using (var fs = new FileStream(convertedFilePath, FileMode.Open))
                {
                    try
                    {
                        var file = DxfFile.Load(fs);
                        Assert.IsType<DxfLine>(file.Entities.Single());
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Error reading Teigha-produced files", exceptions);
            }
        }

        [TeighaConverterExistsFact]
        public void TeighaReadIxMiliaNewFileCompatTest()
        {
            TestTeighaReadIxMiliaGeneratedFile(() =>
            {
                var file = new DxfFile();
                file.Entities.Add(new DxfLine(new DxfPoint(0, 0, 0), new DxfPoint(10, 10, 0)));
                return file;
            });
        }

        [TeighaConverterExistsFact]
        public void TeighaReadIxMiliaNormalizedFileCompatTest()
        {
            TestTeighaReadIxMiliaGeneratedFile(() =>
            {
                var file = Parse(MinimumFileText);
                file.Normalize();
                return file;
            });
        }

        [AutoCadExistsFact]
        public void IxMiliaReadAutoCadTest()
        {
            // use AutoCad to convert a minimum-working-file to each of its supported versions and try to open with IxMilia
            var exceptions = new List<Exception>();

            var tempDir = PrepareTempDirectory("AutoCadTempDir");
            var barePath = Path.Combine(tempDir, "bare.dxf");
            File.WriteAllText(barePath, MinimumFileText);

            var scriptLines = new List<string>();
            scriptLines.Add("FILEDIA 0");
            scriptLines.Add($"DXFIN \"{barePath}\"");
            foreach (var version in new[] { "R12", "2000", "2004", "2007", "2010", "2013" })
            {
                var fullPath = Path.Combine(tempDir, $"result-{version}.dxf");
                scriptLines.Add($"DXFOUT \"{fullPath}\" V {version} 16");
            }

            scriptLines.Add("FILEDIA 1");
            scriptLines.Add("QUIT Y");
            scriptLines.Add("");
            var scriptPath = Path.Combine(tempDir, "script.scr");
            File.WriteAllLines(scriptPath, scriptLines);

            var psi = new ProcessStartInfo();
            psi.FileName = AutoCadExistsFactAttribute.GetPathToAutoCad();
            psi.Arguments = $@"/b ""{scriptPath}""";
            var proc = Process.Start(psi);
            proc.WaitForExit();
            Assert.Equal(0, proc.ExitCode);

            foreach (var resultPath in Directory.EnumerateFiles(tempDir, "result-*.dxf"))
            {
                using (var fs = new FileStream(resultPath, FileMode.Open))
                {
                    try
                    {
                        var file = DxfFile.Load(fs);
                        Assert.IsType<DxfLine>(file.Entities.Single());
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Error reading AutoCad-produced files", exceptions);
            }
        }

        private void TestTeighaReadIxMiliaGeneratedFile(Func<DxfFile> fileGenerator)
        {
            // save a DXF file in all the formats that IxMilia.Dxf supports and try to get Teigha to read all of them
            var inputDir = PrepareTempDirectory("TeighaCompatInputDir");

            // save the minimum file with all versions
            var file = fileGenerator();
            var allIxMiliaVersions = new[]
            {
                DxfAcadVersion.R9,
                DxfAcadVersion.R10,
                DxfAcadVersion.R11,
                DxfAcadVersion.R12,
                DxfAcadVersion.R13,
                DxfAcadVersion.R14,
                DxfAcadVersion.R2000,
                DxfAcadVersion.R2004,
                DxfAcadVersion.R2007,
                DxfAcadVersion.R2010,
                DxfAcadVersion.R2013
            };
            foreach (var version in allIxMiliaVersions)
            {
                file.Header.Version = version;
                var outputPath = Path.Combine(inputDir, $"file.{version}.dxf");
                using (var fs = new FileStream(outputPath, FileMode.Create))
                {
                    file.Save(fs);
                }
            }

            // invoke the Teigha converter
            var errors = new List<string>();
            var teighaVersion = "ACAD2010";
            var outputDir = PrepareTempDirectory("TeighaCompatOutputDir");
            var psi = new ProcessStartInfo();
            psi.FileName = TeighaConverterExistsFactAttribute.GetPathToFileConverter();
            psi.Arguments = $@"""{inputDir}"" ""{outputDir}"" ""{teighaVersion}"" ""DXF"" ""0"" ""1""";
            //                                                                            recurse audit
            var proc = Process.Start(psi);
            proc.WaitForExit();
            Assert.Equal(0, proc.ExitCode);

            // check for any error files
            foreach (var errorFile in Directory.EnumerateFiles(outputDir, "*.err"))
            {
                errors.Add(File.ReadAllText(errorFile));
            }

            if (errors.Count > 0)
            {
                throw new Exception($"Errors reading IxMilia files:\r\n{string.Join("\r\n", errors)}");
            }
        }
    }
}
