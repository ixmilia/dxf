using System;
using System.IO;
using Xunit;

namespace IxMilia.Dxf.Integration.Test
{
    public class ODAConverterExistsFactAttribute : FactAttribute
    {
        private const string _converterExe = "ODAFileConverter.exe";
        private static bool _pathResolved = false;
        private static string _converterPath = null;

        public ODAConverterExistsFactAttribute()
        {
            if (GetPathToFileConverter(throwOnError: false) == null)
            {
                Skip = $"Unable to locate '{_converterExe}'.  Please install from 'https://www.opendesign.com/guestfiles/oda_file_converter' to enable this test.";
            }
        }

        public static string GetPathToFileConverter()
        {
            return GetPathToFileConverter(throwOnError: true);
        }

        private static string GetPathToFileConverter(bool throwOnError)
        {
            if (!_pathResolved)
            {
                _pathResolved = true;
                var environmentVariables = new[]
                {
                    "ProgramFiles(x86)",
                    "ProgramFiles",
                    "ProgramW6432",
                };
                foreach (var environmentVariable in environmentVariables)
                {
                    var programFiles = Environment.GetEnvironmentVariable(environmentVariable);
                    var oda = Path.Combine(programFiles, "ODA");
                    if (Directory.Exists(oda))
                    {
                        foreach (var candidateDir in Directory.EnumerateDirectories(oda, "ODA*"))
                        {
                            var candidatePath = Path.Combine(candidateDir, _converterExe);
                            if (File.Exists(candidatePath))
                            {
                                _converterPath = candidatePath;
                            }
                        }
                    }
                }
            }

            if (_converterPath == null && throwOnError)
            {
                throw new Exception($"Unable to locate '{_converterExe}'.  Please mark this test with the {nameof(ODAConverterExistsFactAttribute)} attribute.");
            }
            else
            {
                return _converterPath;
            }
        }
    }
}
