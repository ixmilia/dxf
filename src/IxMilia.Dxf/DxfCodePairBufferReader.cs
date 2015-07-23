// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace IxMilia.Dxf
{
    internal class DxfBufferReader<T>
    {
        private readonly IEnumerator<T> enumerator;
        private readonly Func<T, bool> isIgnorable;

        public DxfBufferReader(IEnumerable<T> pairs, Func<T, bool> isIgnorable)
        {
            this.enumerator = pairs.GetEnumerator();
            this.isIgnorable = isIgnorable;
            ItemsRemain = enumerator.MoveNext();
        }

        public bool ItemsRemain { get; private set; }

        public T Peek()
        {
            if (!this.ItemsRemain)
            {
                throw new IndexOutOfRangeException("No more items.");
            }

            return enumerator.Current;
        }

        public void Advance()
        {
            ItemsRemain = enumerator.MoveNext();
            while (ItemsRemain && isIgnorable(Peek()))
            {
                ItemsRemain = enumerator.MoveNext();
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
