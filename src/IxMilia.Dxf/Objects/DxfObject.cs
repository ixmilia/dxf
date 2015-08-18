﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace IxMilia.Dxf.Objects
{
    public abstract partial class DxfObject
    {
        public abstract DxfObjectType ObjectType { get; }

        protected virtual DxfAcadVersion MinVersion
        {
            get { return DxfAcadVersion.Min; }
        }

        protected virtual DxfAcadVersion MaxVersion
        {
            get { return DxfAcadVersion.Max; }
        }

        public IEnumerable<DxfCodePair> GetValuePairs(DxfAcadVersion version, bool outputHandles)
        {
            var pairs = new List<DxfCodePair>();
            if (version >= MinVersion && version <= MaxVersion)
            {
                AddValuePairs(pairs, version, outputHandles);
                //AddTrailingCodePairs(pairs, version, outputHandles);
            }

            return pairs;
        }

        internal virtual void PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }
                //else if (pair.Code == DxfCodePairGroup.GroupCodeNumber)
                //{
                //    buffer.Advance();
                //    var groupName = DxfCodePairGroup.GetGroupName(pair.StringValue);
                //    ExtensionDataGroups.Add(DxfCodePairGroup.FromBuffer(buffer, groupName));
                //}
                //else if (pair.Code == (int)DxfXDataType.ApplicationName)
                //{
                //    XDataProtected = DxfXData.FromBuffer(buffer, pair.StringValue);
                //}

                if (!TrySetPair(pair))
                {
                    //ExcessCodePairs.Add(pair);
                }

                buffer.Advance();
            }

            //return PostParse();
        }

        private static void SwallowObject(DxfCodePairBufferReader buffer)
        {
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                    break;
                buffer.Advance();
            }
        }
    }
}