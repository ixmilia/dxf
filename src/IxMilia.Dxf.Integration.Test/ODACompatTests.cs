using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Objects;
using IxMilia.Dxf.Test;
using Xunit;

namespace IxMilia.Dxf.Integration.Test
{
    public class ODACompatTests : CompatTestsBase
    {
        [ODAConverterExistsFact]
        public void IxMiliaReadODATest()
        {
            // use ODA to convert a minimum-working-file to each of its supported versions and try to open with IxMilia
            var exceptions = new List<Exception>();
            var versions = new[]
            {
                DxfAcadVersion.R9,
                DxfAcadVersion.R10,
                DxfAcadVersion.R12,
                DxfAcadVersion.R13,
                DxfAcadVersion.R14,
                DxfAcadVersion.R2000,
                DxfAcadVersion.R2004,
                DxfAcadVersion.R2007,
                DxfAcadVersion.R2010,
                DxfAcadVersion.R2013,
                DxfAcadVersion.R2018,
            };
            foreach (var desiredVersion in versions)
            {
                using (var input = new ManageTemporaryDirectory())
                using (var output = new ManageTemporaryDirectory())
                {
                    var inputDir = input.DirectoryPath;
                    var outputDir = output.DirectoryPath;
                    var barePath = Path.Combine(inputDir, "bare.dxf");
                    File.WriteAllText(barePath, MinimumFileText);
                    AssertODAConvert(inputDir, outputDir, desiredVersion);

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
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Error reading ODA-produced files", exceptions);
            }
        }

        [ODAConverterExistsFact]
        public void ODAReadIxMiliaNewFileCompatTest()
        {
            TestODAReadIxMiliaGeneratedFile(() =>
            {
                var file = new DxfFile();
                file.Entities.Add(new DxfLine(new DxfPoint(0, 0, 0), new DxfPoint(10, 10, 0)));
                return file;
            });
        }

        [ODAConverterExistsFact]
        public void ODAReadIxMiliaNormalizedFileCompatTest()
        {
            TestODAReadIxMiliaGeneratedFile(() =>
            {
                var file = Parse(MinimumFileText);
                return file;
            });
        }

        [ODAConverterExistsFact]
        public void ODAReadAllEntitiesTest()
        {
            // create a file with all entities and ensure ODA can read it
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2013;
            var assembly = typeof(DxfFile).GetTypeInfo().Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (ReaderWriterTests.IsEntityOrDerived(type))
                {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        // add the entity with its default initialized values
                        var entity = (DxfEntity)ctor.Invoke(new object[0]);
                        file.Entities.Add(entity);
                    }
                }
            }

            using (var input = new ManageTemporaryDirectory())
            using (var output = new ManageTemporaryDirectory())
            {
                var inputFile = Path.Combine(input.DirectoryPath, "file.dxf");
                using (var fs = new FileStream(inputFile, FileMode.Create))
                {
                    file.Save(fs);
                }

                AssertODAConvert(input.DirectoryPath, output.DirectoryPath, DxfAcadVersion.R2013);
            }
        }

        [ODAConverterExistsFact]
        public void ODAReadAllObjectsTest()
        {
            // create a file with all objects and ensure ODA can read it
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2013;
            var assembly = typeof(DxfFile).GetTypeInfo().Assembly;
            foreach (var type in assembly.GetTypes().Where(ReaderWriterTests.IsObjectOrDerived))
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    // add the object with its default initialized values
                    var obj = (DxfObject)ctor.Invoke(new object[0]);
                    file.Objects.Add(obj);
                }
            }

            using (var input = new ManageTemporaryDirectory())
            using (var output = new ManageTemporaryDirectory())
            {
                var inputFile = Path.Combine(input.DirectoryPath, "file.dxf");
                using (var fs = new FileStream(inputFile, FileMode.Create))
                {
                    file.Save(fs);
                }

                AssertODAConvert(input.DirectoryPath, output.DirectoryPath, DxfAcadVersion.R2013);
            }
        }

        [ODAConverterExistsFact]
        public void ODACanReadSpecificEntitiesTest()
        {
            RoundTripDimensionWithXData(RoundTripFileThroughODA);
        }

        private DxfFile RoundTripFileThroughODA(DxfFile file)
        {
            using (var input = new ManageTemporaryDirectory())
            using (var output = new ManageTemporaryDirectory())
            {
                file.Save(Path.Combine(input.DirectoryPath, "drawing.dxf"));
                AssertODAConvert(input.DirectoryPath, output.DirectoryPath, file.Header.Version);
                var result = DxfFile.Load(Path.Combine(output.DirectoryPath, "drawing.dxf"));
                return result;
            }
        }

        private void TestODAReadIxMiliaGeneratedFile(Func<DxfFile> fileGenerator)
        {
            // save a DXF file in all the formats that IxMilia.Dxf supports and try to get ODA to read all of them
            using (var input = new ManageTemporaryDirectory())
            {
                var inputDir = input.DirectoryPath;

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
                    DxfAcadVersion.R2013,
                    DxfAcadVersion.R2018,
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

                // invoke the ODA converter
                using (var output = new ManageTemporaryDirectory())
                {
                    var outputDir = output.DirectoryPath;
                    AssertODAConvert(inputDir, outputDir, DxfAcadVersion.R2010);
                }
            }
        }

        private void AssertODAConvert(string inputDirectory, string outputDirectory, DxfAcadVersion desiredVersion)
        {
            WaitForProcess(ODAConverterExistsFactAttribute.GetPathToFileConverter(), GenerateODAArguments(inputDirectory, outputDirectory, desiredVersion));
            var errors = Directory.EnumerateFiles(outputDirectory, "*.err").Select(path => path + ":" + Environment.NewLine + File.ReadAllText(path)).ToList();
            // TODO: gather the files that couldn't be converted
            if (errors.Count > 0)
            {
                throw new Exception("ODA error converting files: " + string.Join("", errors));
            }
        }

        private string GenerateODAArguments(string inputDirectory, string outputDirectory, DxfAcadVersion desiredVersion)
        {
            string odaVersion;
            switch (desiredVersion)
            {
                case DxfAcadVersion.R9:
                    odaVersion = "ACAD9";
                    break;
                case DxfAcadVersion.R10:
                    odaVersion = "ACAD10";
                    break;
                case DxfAcadVersion.R12:
                    odaVersion = "ACAD12";
                    break;
                case DxfAcadVersion.R13:
                    odaVersion = "ACAD13";
                    break;
                case DxfAcadVersion.R14:
                    odaVersion = "ACAD14";
                    break;
                case DxfAcadVersion.R2000:
                    odaVersion = "ACAD2000";
                    break;
                case DxfAcadVersion.R2004:
                    odaVersion = "ACAD2004";
                    break;
                case DxfAcadVersion.R2007:
                    odaVersion = "ACAD2007";
                    break;
                case DxfAcadVersion.R2010:
                    odaVersion = "ACAD2010";
                    break;
                case DxfAcadVersion.R2013:
                    odaVersion = "ACAD2013";
                    break;
                case DxfAcadVersion.R2018:
                    odaVersion = "ACAD2018";
                    break;
                default:
                    throw new InvalidOperationException("Unsupported ODA version " + desiredVersion);
            }

            //                                                                          recurse audit
            return $@"""{inputDirectory}"" ""{outputDirectory}"" ""{odaVersion}"" ""DXF"" ""0"" ""1""";
        }
    }
}
