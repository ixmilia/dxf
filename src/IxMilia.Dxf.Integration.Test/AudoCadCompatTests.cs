using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Test;
using Xunit;

namespace IxMilia.Dxf.Integration.Test
{
    public class AudoCadCompatTests : CompatTestsBase
    {
        [AutoCadExistsFact]
        public void IxMiliaReadAutoCadTest()
        {
            // use AutoCad to convert a minimum-working-file to each of its supported versions and try to open with IxMilia
            var exceptions = new List<Exception>();

            using (var directory = new ManageTemporaryDirectory())
            {
                var tempDir = directory.DirectoryPath;
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

                ExecuteAutoCadScript(scriptPath);

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
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Error reading AutoCad-produced files", exceptions);
            }
        }

        [AutoCadExistsFact]
        public void AutoCadReadIxMiliaFileCompatTest()
        {
            // save a DXF file in all the formats that IxMilia.Dxf and AutoCAD support and try to get AutoCAD to read all of them
            using (var directory = new ManageTemporaryDirectory())
            {
                var tempDir = directory.DirectoryPath;
                var versions = new[]
                {
                    DxfAcadVersion.R9,
                    DxfAcadVersion.R10,
                    DxfAcadVersion.R11,
                    DxfAcadVersion.R12,
                    //DxfAcadVersion.R13,
                    DxfAcadVersion.R14,
                    DxfAcadVersion.R2000,
                    DxfAcadVersion.R2004,
                    DxfAcadVersion.R2007,
                    DxfAcadVersion.R2010,
                    DxfAcadVersion.R2013
                };

                // save the minimal file with all versions
                var file = new DxfFile();
                var text = new DxfText(DxfPoint.Origin, 2.0, "");
                file.Entities.Add(text);
                foreach (var version in versions)
                {
                    var fileName = $"file.{version}.dxf";
                    file.Header.Version = version;
                    text.Value = version.ToString();
                    var outputPath = Path.Combine(tempDir, fileName);
                    using (var fs = new FileStream(outputPath, FileMode.Create))
                    {
                        file.Save(fs);
                    }
                }

                // open each file in AutoCAD and try to write it back out
                var lines = new List<string>();
                lines.Add("FILEDIA 0");
                foreach (var version in versions)
                {
                    lines.Add("ERASE ALL ");
                    lines.Add($"DXFIN \"{Path.Combine(tempDir, $"file.{version}.dxf")}\"");
                    lines.Add($"DXFOUT \"{Path.Combine(tempDir, $"result.{version}.dxf")}\" V R12 16");
                }

                lines.Add("FILEDIA 1");
                lines.Add("QUIT Y");

                // create and execute the script
                var scriptPath = Path.Combine(tempDir, "script.scr");
                File.WriteAllLines(scriptPath, lines);

                ExecuteAutoCadScript(scriptPath);

                // check each resultant file for the correct version and text
                foreach (var version in versions)
                {
                    DxfFile dxf;
                    using (var fs = new FileStream(Path.Combine(tempDir, $"result.{version}.dxf"), FileMode.Open))
                    {
                        dxf = DxfFile.Load(fs);
                    }

                    Assert.Equal(version.ToString(), ((DxfText)dxf.Entities.Single()).Value);
                }
            }
        }

        [AutoCadExistsFact]
        public void AutoCadReadAllEntitiesTest()
        {
            // TODO: make these work with AutoCAD
            var unsupportedTypes = new[]
            {
                // unsupported because I need to write more information with them
                typeof(DxfHatch), // need to fill in the data
                typeof(DxfInsert), // need a block to insert
                typeof(DxfLeader), // needs vertices
                typeof(DxfMLine), // need to set MLINESTYLE and MLINESTYLE dictionary
                typeof(DxfDgnUnderlay), // AcDbUnderlayDefinition object ID must be set
                typeof(DxfDwfUnderlay), // AcDbUnderlayDefinition object ID must be set
                typeof(DxfPdfUnderlay), // AcDbUnderlayDefinition object ID must be set
                typeof(DxfSpline), // need to supply control/fit points
                typeof(DxfVertex), // can't write a lone vertex?

                // unsupported for other reasons TBD
                typeof(Dxf3DSolid),
                typeof(DxfAttribute),
                typeof(DxfAttributeDefinition),
                typeof(DxfBody),
                typeof(DxfHelix), // acad expects AcDbSpline?
                typeof(DxfLight),
                typeof(DxfMText),
                typeof(DxfRegion),
                typeof(DxfProxyEntity),
                typeof(DxfShape),
                typeof(DxfTolerance),
            };

            // create a file with all entities and ensure AutoCAD can read it
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2010;
            var assembly = typeof(DxfFile).GetTypeInfo().Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (ReaderWriterTests.IsEntityOrDerived(type) && type.GetTypeInfo().BaseType != typeof(DxfDimensionBase) && !unsupportedTypes.Contains(type))
                {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        // add the entity with its default initialized values
                        var entity = (DxfEntity)ctor.Invoke(new object[0]);
                        file.Entities.Add(entity);

                        if (entity is DxfText)
                        {
                            // set an explicit value to ensure that it could be round-tripped
                            ((DxfText)entity).Value = "sample text";
                        }
                    }
                }
            }

            using (var directory = new ManageTemporaryDirectory())
            {
                var sampleFilePath = Path.Combine(directory.DirectoryPath, "file.dxf");
                using (var fs = new FileStream(sampleFilePath, FileMode.Create))
                {
                    file.Save(fs);
                }

                var scriptFilePath = Path.Combine(directory.DirectoryPath, "script.scr");
                var outputFilePath = Path.Combine(directory.DirectoryPath, "result.dxf");
                File.WriteAllLines(scriptFilePath, new[]
                {
                    "FILEDIA 0",
                    $"DXFIN \"{sampleFilePath}\"",
                    $"DXFOUT \"{outputFilePath}\" 16",
                    "FILEDIA 1",
                    "QUIT Y"
                });
                ExecuteAutoCadScript(scriptFilePath);

                // read file back in and confirm DxfText value
                DxfFile resultFile;
                using (var fs = new FileStream(outputFilePath, FileMode.Open))
                {
                    resultFile = DxfFile.Load(fs);
                }

                var text = resultFile.Entities.OfType<DxfText>().Single();
                Assert.Equal("sample text", text.Value);
            }
        }

        [AutoCadExistsFact]
        public void AutoCadCanReadSpecificEntitiesTest()
        {
            RoundTripDimensionWithXData(RoundTripFileThroughAutoCad);
        }

        private DxfFile RoundTripFileThroughAutoCad(DxfFile file)
        {
            using (var temp = new ManageTemporaryDirectory())
            {
                var inputFile = Path.Combine(temp.DirectoryPath, "input.dxf");
                var outputFile = Path.Combine(temp.DirectoryPath, "output.dxf");
                var scriptFile = Path.Combine(temp.DirectoryPath, "script.scr");
                file.Save(inputFile);

                var lines = new List<string>
                {
                    "FILEDIA 0",
                    $"DXFIN \"{inputFile}\"",
                    $"DXFOUT \"{outputFile}\" 16",
                    "FILEDIA 1",
                    "QUIT Y"
                };
                File.WriteAllLines(scriptFile, lines);
                ExecuteAutoCadScript(scriptFile);

                var result = DxfFile.Load(outputFile);
                return result;
            }
        }

        private void ExecuteAutoCadScript(string pathToScript)
        {
            WaitForProcess(AutoCadExistsFactAttribute.GetPathToAutoCad(), $"/b \"{pathToScript}\"");
            // TODO: kill all instances of senddmp.exe and fail if present
        }
    }
}
