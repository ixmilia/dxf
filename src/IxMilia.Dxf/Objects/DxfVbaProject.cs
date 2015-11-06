// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfVbaProject
    {
        public byte[] Data { get; set; }

        private IEnumerable<string> GetHexStrings()
        {
            if (Data == null)
                yield break;

            var pos = 0;
            while (pos < Data.Length)
            {
                var slice = Data.Skip(pos).Take(128).ToArray();
                pos += slice.Length;
                yield return DxfCommonConverters.HexBytes(slice);
            }
        }

        protected override DxfObject PostParse()
        {
            Data = _hexData.SelectMany(h => DxfCommonConverters.HexBytes(h)).ToArray();
            _hexData.Clear();
            return this;
        }
    }
}
