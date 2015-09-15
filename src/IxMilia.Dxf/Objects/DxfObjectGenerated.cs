﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// The contents of this file are automatically generated by a tool, and should not be directly modified.

using System;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf.Objects
{

    public enum DxfObjectType
    {
        AcadProxyObject,
        AcdbDictionary,
        AcdbDictionaryWithDefault,
        AcdbPlaceHolder,
        DataTable,
        DimensionAssociativity,
        Field,
        GeoData,
        Group,
        IdBuffer,
    }

    /// <summary>
    /// DxfObject class
    /// </summary>
    public partial class DxfObject : IDxfHasHandle
    {
        public uint Handle { get; set; }
        public uint OwnerHandle { get; set; }

        public string ObjectTypeString
        {
            get
            {
                switch (ObjectType)
                {
                    case DxfObjectType.AcadProxyObject:
                        return "ACAD_PROXY_OBJECT";
                    case DxfObjectType.AcdbDictionaryWithDefault:
                        return "ACDBDICTIONARYWDFLT";
                    case DxfObjectType.AcdbPlaceHolder:
                        return "ACDBPLACEHOLDER";
                    case DxfObjectType.DataTable:
                        return "DATATABLE";
                    case DxfObjectType.AcdbDictionary:
                        return "DICTIONARY";
                    case DxfObjectType.DimensionAssociativity:
                        return "DIMASSOC";
                    case DxfObjectType.Field:
                        return "FIELD";
                    case DxfObjectType.GeoData:
                        return "GEODATA";
                    case DxfObjectType.Group:
                        return "GROUP";
                    case DxfObjectType.IdBuffer:
                        return "IDBUFFER";
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        protected DxfObject(DxfObject other)
            : this()
        {
            this.Handle = other.Handle;
            this.OwnerHandle = other.OwnerHandle;
        }

        protected virtual void Initialize()
        {
            this.Handle = 0u;
            this.OwnerHandle = 0u;
        }

        protected virtual void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            pairs.Add(new DxfCodePair(0, ObjectTypeString));
            if (outputHandles)
            {
                pairs.Add(new DxfCodePair(5, UIntHandle(this.Handle)));
            }

            AddExtensionValuePairs(pairs, version, outputHandles);
            if (version >= DxfAcadVersion.R2000)
            {
                pairs.Add(new DxfCodePair(330, UIntHandle(this.OwnerHandle)));
            }

        }

        internal virtual bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 5:
                    this.Handle = UIntHandle(pair.StringValue);
                    break;
                case 330:
                    this.OwnerHandle = UIntHandle(pair.StringValue);
                    break;
                default:
                    return false;
            }

            return true;
        }

        internal static DxfObject FromBuffer(DxfCodePairBufferReader buffer)
        {
            var first = buffer.Peek();
            buffer.Advance();
            DxfObject obj;
            switch (first.StringValue)
            {
                case "ACAD_PROXY_OBJECT":
                    obj = new DxfAcadProxyObject();
                    break;
                case "ACDBDICTIONARYWDFLT":
                    obj = new DxfAcdbDictionaryWithDefault();
                    break;
                case "ACDBPLACEHOLDER":
                    obj = new DxfAcdbPlaceHolder();
                    break;
                case "DATATABLE":
                    obj = new DxfDataTable();
                    break;
                case "DICTIONARY":
                    obj = new DxfDictionary();
                    break;
                case "DIMASSOC":
                    obj = new DxfDimensionAssociativity();
                    break;
                case "FIELD":
                    obj = new DxfField();
                    break;
                case "GEODATA":
                    obj = new DxfGeoData();
                    break;
                case "GROUP":
                    obj = new DxfGroup();
                    break;
                case "IDBUFFER":
                    obj = new DxfIdBuffer();
                    break;
                default:
                    SwallowObject(buffer);
                    obj = null;
                    break;
            }

            if (obj != null)
            {
                obj = obj.PopulateFromBuffer(buffer);
            }

            return obj;
        }
    }

}

// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This line is required for T4 template generation to work. 
// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This line is required for T4 template generation to work. 

