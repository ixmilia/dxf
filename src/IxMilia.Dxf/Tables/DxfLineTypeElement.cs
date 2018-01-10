// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf
{
    public class DxfLineTypeElement : IDxfItemInternal
    {
        public double DashDotSpaceLength { get; set; }
        public int ComplexFlags { get; set; }
        public int ShapeNumber { get; set; }
        public IList<double> ScaleValues { get; } = new List<double>();

        /// <summary>
        /// The rotation angle in radians.
        /// </summary>
        public double RotationAngle { get; set; }

        public IList<DxfVector> Offsets { get; } = new List<DxfVector>();
        public string TextString { get; set; }

        public DxfStyle Style { get { return StylePointer.Item as DxfStyle; } set { StylePointer.Item = value; } }

        public bool IsRotationAbsolute
        {
            get { return DxfHelpers.GetFlag(ComplexFlags, 1); }
            set
            {
                var flags = ComplexFlags;
                DxfHelpers.SetFlag(value, ref flags, 1);
                ComplexFlags = flags;
            }
        }

        public bool IsEmbeddedElementAString
        {
            get { return DxfHelpers.GetFlag(ComplexFlags, 2); }
            set
            {
                var flags = ComplexFlags;
                DxfHelpers.SetFlag(value, ref flags, 2);
                ComplexFlags = flags;
            }
        }

        public bool IsEmbeddedElementAShape
        {
            get { return DxfHelpers.GetFlag(ComplexFlags, 4); }
            set
            {
                var flags = ComplexFlags;
                DxfHelpers.SetFlag(value, ref flags, 4);
                ComplexFlags = flags;
            }
        }

        public DxfLineTypeElement()
        {
            DashDotSpaceLength = 0.0;
            ComplexFlags = 0;
            ShapeNumber = 0;
            RotationAngle = 0.0;
            TextString = null;
        }

        internal DxfPointer StylePointer { get; } = new DxfPointer();

        internal void AddValuePairs(List<DxfCodePair> pairs)
        {
            pairs.Add(new DxfCodePair(49, DashDotSpaceLength));
            pairs.Add(new DxfCodePair(74, (short)ComplexFlags));
            if (ComplexFlags != 0)
            {
                var value = IsEmbeddedElementAString ? 0 : ShapeNumber;
                pairs.Add(new DxfCodePair(75, (short)value));
                if (StylePointer.Handle != 0u)
                {
                    pairs.Add(new DxfCodePair(340, DxfCommonConverters.UIntHandle(StylePointer.Handle)));
                }
            }

            pairs.AddRange(ScaleValues.Select(s => new DxfCodePair(46, s)));
            if (IsEmbeddedElementAShape || IsEmbeddedElementAString)
            {
                pairs.Add(new DxfCodePair(50, RotationAngle));
            }

            foreach (var offset in Offsets)
            {
                pairs.Add(new DxfCodePair(44, offset.X));
                pairs.Add(new DxfCodePair(45, offset.Y));
            }

            if (IsEmbeddedElementAString)
            {
                pairs.Add(new DxfCodePair(9, TextString));
            }
        }

        uint IDxfItemInternal.Handle { get; set; }
        uint IDxfItemInternal.OwnerHandle { get; set; }
        public IDxfItem Owner { get; private set; }

        void IDxfItemInternal.SetOwner(IDxfItem owner)
        {
            Owner = owner;
        }

        IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()
        {
            yield return StylePointer;
        }

        IEnumerable<IDxfItemInternal> IDxfItemInternal.GetChildItems()
        {
            return ((IDxfItemInternal)this).GetPointers().Select(p => (IDxfItemInternal)p.Item);
        }
    }
}
