using System;
using System.IO;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class AutoCadExistsFactAttribute : FactAttribute
    {
        private const string _converterExe = "acad.exe";
        private static bool _pathResolved = false;
        private static string _converterPath = null;

        public AutoCadExistsFactAttribute()
        {
            if (GetPathToAutoCad(throwOnError: false) == null)
            {
                Skip = $"Unable to locate '{_converterExe}', test will be skipped";
            }
        }

        public static string GetPathToAutoCad()
        {
            return GetPathToAutoCad(throwOnError: true);
        }

        private static string GetPathToAutoCad(bool throwOnError)
        {
            if (!_pathResolved)
            {
                _pathResolved = true;
                var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ??
                    Environment.GetEnvironmentVariable("ProgramFiles");
                if (programFiles.EndsWith(" (x86)"))
                {
                    // hack when running in VS
                    programFiles = programFiles.Substring(0, programFiles.Length - 6);
                }

                var autodesk = Path.Combine(programFiles, "Autodesk");
                if (Directory.Exists(autodesk))
                {
                    foreach (var candidateDir in Directory.EnumerateDirectories(autodesk, "AutoCAD*"))
                    {
                        var candidatePath = Path.Combine(candidateDir, _converterExe);
                        if (File.Exists(candidatePath))
                        {
                            _converterPath = candidatePath;
                        }
                    }
                }
            }

            if (_converterPath == null && throwOnError)
            {
                throw new Exception($"Unable to locate '{_converterExe}'.  Please mark this test with the {nameof(AutoCadExistsFactAttribute)} attribute.");
            }
            else
            {
                return _converterPath;
            }
        }
    }
}
