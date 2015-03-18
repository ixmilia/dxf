// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace IxMilia.Dxf
{
    internal class DxfBufferReader<T>
    {
        private T[] items;
        private int position;
        private Func<T, bool> isIgnorable;

        public int Position { get { return position; } }

        public DxfBufferReader(IEnumerable<T> pairs, Func<T, bool> isIgnorable)
        {
            this.items = pairs.ToArray();
            this.position = 0;
            this.isIgnorable = isIgnorable;
        }

        public bool ItemsRemain
        {
            get
            {
                return this.position < this.items.Length;
            }
        }

        public T Peek()
        {
            if (!this.ItemsRemain)
            {
                throw new DxfReadException("No more items.");
            }

            return this.items[this.position];
        }

        public void Advance()
        {
            this.position++;
            while (ItemsRemain && isIgnorable(Peek()))
            {
                this.position++;
            }
        }
    }

    internal class DxfCodePairBufferReader : DxfBufferReader<DxfCodePair>
    {
        public DxfCodePairBufferReader(IEnumerable<DxfCodePair> pairs)
            : base(pairs, (pair) => pair.Code == 999)
        {
        }
    }
}
