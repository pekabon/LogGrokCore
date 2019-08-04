using System.Collections;
using System.Collections.Generic;
using LogGrokCore.Data.IndexTree;

namespace LogGrokCore.Data.Tests
{
    public class TestIndexTreeLeaf : LeafOrNode<int, TestIndexTreeLeaf >, ILeaf<int, TestIndexTreeLeaf>
    {
        private const int Capacity = 2;
        private readonly List<int> _values = new List<int>(Capacity);
        private readonly int _firstValueIndex;
        public TestIndexTreeLeaf(int value, int index)
        {
            _values.Add(value);
            _firstValueIndex = index;
        }

        public TestIndexTreeLeaf? Add(int value, int valueIndex)
        {
            if (_values.Count >= Capacity)
            {
                Next = new TestIndexTreeLeaf(value, valueIndex);
                return Next;
            }

            _values.Add(value);
            return null;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int FirstValue => _values[0];
        public override int MinIndex => _firstValueIndex;
        public override IEnumerable<int> GetEnumerableFromIndex(int index)
        {
            return this.GetEnumerableFromIndex<int, TestIndexTreeLeaf>(index);
        }

        public int this[int index] => _values[index];

        public int Count => _values.Count;

        public TestIndexTreeLeaf? Next { get; private set; }
    }
}