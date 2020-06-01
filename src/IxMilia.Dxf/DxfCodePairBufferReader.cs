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
            Advance();
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
        private IDxfCodePairReader reader;

        public DxfCodePairBufferReader(IDxfCodePairReader reader)
            : base(reader.GetCodePairs(), (pair) => pair.Code == 999)
        {
            this.reader = reader;
        }

        public void SetUtf8Reader()
        {
            reader.SetUtf8Reader();
        }
    }
}
