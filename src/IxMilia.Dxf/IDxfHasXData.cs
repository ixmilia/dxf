using System.Collections.Generic;

namespace IxMilia.Dxf
{
    public interface IDxfHasXData
    {
        IList<DxfCodePairGroup> ExtensionDataGroups { get; }
        IDictionary<string, DxfXDataApplicationItemCollection> XData { get; }
    }

    internal static class DxfXDataHelper
    {
        public static bool TrySetExtensionData<THasXData>(this THasXData hasXData, DxfCodePair pair, DxfCodePairBufferReader buffer)
            where THasXData : IDxfHasXData
        {
            if (pair.Code == DxfCodePairGroup.GroupCodeNumber && pair.StringValue.StartsWith("{"))
            {
                buffer.Advance();
                var groupName = DxfCodePairGroup.GetGroupName(pair.StringValue);
                hasXData.ExtensionDataGroups.Add(DxfCodePairGroup.FromBuffer(buffer, groupName));
                return true;
            }
            else if (pair.Code >= 1000)
            {
                buffer.Advance();
                DxfXData.PopulateFromBuffer(buffer, hasXData.XData, pair.StringValue);
                return true;
            }

            return false;
        }
    }
}
