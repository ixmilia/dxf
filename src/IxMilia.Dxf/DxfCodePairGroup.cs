// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf
{
    public interface IDxfCodePairOrGroup
    {
        bool IsCodePair { get; }
    }

    public class DxfCodePairGroup : IDxfCodePairOrGroup
    {
        internal const int GroupCodeNumber = 102;

        public string GroupName { get; set; }

        public List<IDxfCodePairOrGroup> Items { get; private set; }

        public bool IsCodePair { get { return false; } }

        public bool IsSingleton { get; private set; }

        public DxfCodePairGroup()
            : this(null, null)
        {
        }

        public DxfCodePairGroup(string groupName, IEnumerable<IDxfCodePairOrGroup> items)
        {
            GroupName = groupName;
            Items = items == null ? new List<IDxfCodePairOrGroup>() : items.ToList();
        }

        internal void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            if (Items.Count == 0)
            {
                return;
            }

            if (IsSingleton)
            {
                pairs.Add(new DxfCodePair(GroupCodeNumber, GroupName));
                pairs.Add((DxfCodePair)Items.Single());
            }
            else
            {

                pairs.Add(new DxfCodePair(GroupCodeNumber, "{" + GroupName));
                foreach (var item in Items)
                {
                    if (item.IsCodePair)
                    {
                        pairs.Add((DxfCodePair)item);
                    }
                    else
                    {
                        ((DxfCodePairGroup)item).AddValuePairs(pairs, version, outputHandles);
                    }
                }

                pairs.Add(new DxfCodePair(GroupCodeNumber, "}"));
            }
        }

        internal static DxfCodePairGroup FromBuffer(DxfCodePairBufferReader buffer, string groupName)
        {
            var items = new List<IDxfCodePairOrGroup>();
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    Debug.Assert(false, "Unexpected end of section/file while parsing group.");
                    break;
                }

                buffer.Advance();
                if (pair.Code == GroupCodeNumber)
                {
                    if (pair.StringValue == "}")
                    {
                        // end of group
                        break;
                    }
                    else if (pair.StringValue.StartsWith("{"))
                    {
                        // nested group
                        var subGroupName = GetGroupName(pair.StringValue);
                        var subGroup = FromBuffer(buffer, subGroupName);
                        items.Add(subGroup);
                    }
                    else
                    {
                        throw new DxfReadException("Unexpected code pair group text", pair);
                    }
                }
                else
                {
                    items.Add(pair);
                }
            }

            return new DxfCodePairGroup(groupName, items);
        }

        public static DxfCodePairGroup CreateSingletonGroup(string controlString, DxfCodePair pair)
        {
            return new DxfCodePairGroup(controlString, new[] { pair }) { IsSingleton = true };
        }

        internal static bool IsSingletonGroup(string controlString)
        {
            return controlString.StartsWith("{");
        }

        internal static string GetGroupName(string controlString)
        {
            Debug.Assert(IsSingletonGroup(controlString));
            return controlString.Substring(1);
        }
    }
}
