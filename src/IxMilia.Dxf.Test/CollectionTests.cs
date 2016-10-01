// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using IxMilia.Dxf.Collections;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class CollectionTests
    {
        [Fact]
        public void MinimumCountIsZeroTest()
        {
            var list = new ListWithPredicates<int>(null, 0);
            list.Add(0);
            list.Clear();
        }

        [Fact]
        public void MinimumCountIsNonZeroTest()
        {
            Assert.Throws<InvalidOperationException>(() => new ListWithPredicates<int>(null, 3));
            var list = new ListWithPredicates<int>(null, 3, 1, 2, 3);
            Assert.Throws<InvalidOperationException>(() => list.RemoveAt(0));
            list.Add(4);
            list.Add(5);
            list.RemoveAt(0);
        }

        [Fact]
        public void ListWithPredicateTest()
        {
            var list = new ListWithPredicates<int>(i => i > 5, 0);
            list.Add(6);
            list.Add(7);
            Assert.Throws<InvalidOperationException>(() => list.Add(5));
        }
    }
}
