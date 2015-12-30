// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace IxMilia.Dxf
{
    public interface IDxfHasXData
    {
        List<DxfCodePairGroup> ExtensionDataGroups { get; }
    }

    internal interface IDxfHasXDataHidden
    {
        DxfXData XDataHidden { get; set; }
    }

    internal static class DxfXDataHelper
    {
        public static bool TrySetExtensionData<THasXData>(this THasXData hasXData, DxfCodePair pair, DxfCodePairBufferReader buffer)
            where THasXData : IDxfHasXData, IDxfHasXDataHidden
        {
            if (pair.Code == DxfCodePairGroup.GroupCodeNumber && pair.StringValue.StartsWith("{"))
            {
                buffer.Advance();
                var groupName = DxfCodePairGroup.GetGroupName(pair.StringValue);
                hasXData.ExtensionDataGroups.Add(DxfCodePairGroup.FromBuffer(buffer, groupName));
                return true;
            }
            else if (pair.Code == (int)DxfXDataType.ApplicationName)
            {
                buffer.Advance();
                hasXData.XDataHidden = DxfXData.FromBuffer(buffer, pair.StringValue);
                return true;
            }

            return false;
        }
    }
}
