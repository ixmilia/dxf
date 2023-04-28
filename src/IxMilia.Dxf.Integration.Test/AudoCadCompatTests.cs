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
                foreach (var version in new[] { "R12", "2000", "2004", "2007", "2010", "2013" })
                {
                    var fullPath = Path.Combine(tempDir, $"result-{version}.dxf");
                    scriptLines.Add($"DXFOUT \"{fullPath}\" V {version} 16");
                }

                scriptLines.Add("QUIT Y");
                scriptLines.Add("");
                var scriptPath = Path.Combine(tempDir, "script.scr");
                File.WriteAllLines(scriptPath, scriptLines);

                ExecuteAutoCadScriptOnDrawing(scriptPath, barePath);

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
                    file.Save(outputPath);

                    // open thefile in AutoCAD and try to write it back out
                    var scriptLines = new List<string>()
                    {
                        $"DXFOUT \"{Path.Combine(tempDir, $"result.{version}.dxf")}\" V R12 16",
                        "QUIT Y"
                    };
                    var scriptPath = Path.Combine(tempDir, $"script.{version}.scr");
                    File.WriteAllLines(scriptPath, scriptLines);

                    ExecuteAutoCadScriptOnDrawing(scriptPath, outputPath);

                    // check the resultant file for the correct version set
                    var dxf = DxfFile.Load(Path.Combine(tempDir, $"result.{version}.dxf"));
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
                    $"DXFOUT \"{outputFilePath}\" 16",
                    "QUIT Y"
                });
                ExecuteAutoCadScriptOnDrawing(scriptFilePath, sampleFilePath);

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
                    $"DXFOUT \"{outputFile}\" 16",
                };
                File.WriteAllLines(scriptFile, lines);
                WaitForProcess(AutoCadExistsFactAttribute.GetPathToAutoCad(), $"/i \"{inputFile}\" /s \"{scriptFile}\"");

                var result = DxfFile.Load(outputFile);
                return result;
            }
        }

        private void ExecuteAutoCadScriptOnDrawing(string pathToScript, string pathToInputDrawing)
        {
            WaitForProcess(AutoCadExistsFactAttribute.GetPathToAutoCad(), $"/i \"{pathToInputDrawing}\" /s \"{pathToScript}\"");
            // TODO: kill all instances of senddmp.exe and fail if present
        }
    }
}
