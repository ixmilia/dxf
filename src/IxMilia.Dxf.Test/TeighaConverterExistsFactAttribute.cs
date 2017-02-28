// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class TeighaConverterExistsFactAttribute : FactAttribute
    {
        private const string _converterExe = "TeighaFileConverter.exe";
        private static bool _pathResolved = false;
        private static string _converterPath = null;

        public TeighaConverterExistsFactAttribute()
        {
            if (GetPathToFileConverter(throwOnError: false) == null)
            {
                Skip = $"Unable to locate '{_converterExe}'.  Please install from 'https://www.opendesign.com/guestfiles/TeighaFileConverter' to enable this test.";
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
                var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ??
                    Environment.GetEnvironmentVariable("ProgramFiles");
                var oda = Path.Combine(programFiles, "ODA");
                if (Directory.Exists(oda))
                {
                    foreach (var candidateDir in Directory.EnumerateDirectories(oda, "Teigha*"))
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
                throw new Exception($"Unable to locate '{_converterExe}'.  Please mark this test with the {nameof(TeighaConverterExistsFactAttribute)} attribute.");
            }
            else
            {
                return _converterPath;
            }
        }
    }
}
