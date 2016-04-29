// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class ConditionalFileExistsFactAttribute : FactAttribute
    {
        public ConditionalFileExistsFactAttribute(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Skip = $"File '{filePath}' could not be found";
            }
        }
    }
}
