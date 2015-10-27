// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// The contents of this file are automatically generated by a tool, and should not be directly modified.

using System;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf.Objects
{

    /// <summary>
    /// DxfIdBuffer class
    /// </summary>
    public partial class DxfIdBuffer : DxfObject
    {
        public override DxfObjectType ObjectType { get { return DxfObjectType.IdBuffer; } }

        public List<uint> EntityHandles { get; private set; }

        public DxfIdBuffer()
            : base()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.EntityHandles = new List<uint>();
        }

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            base.AddValuePairs(pairs, version, outputHandles);
            pairs.Add(new DxfCodePair(100, "AcDbIdBuffer"));
            pairs.AddRange(this.EntityHandles.Select(p => new DxfCodePair(330, p)));
        }

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 330:
                    this.EntityHandles.Add(UIntHandle(pair.StringValue));
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }
    }

}