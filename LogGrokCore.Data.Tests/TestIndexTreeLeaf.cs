using System.Collections;
using System.Collections.Generic;
using LogGrokCore.Data.IndexTree;

namespace LogGrokCore.Data.Tests
{
    public class TestIndexTreeLeaf : LeafOrNode<int, TestIndexTreeLeaf >, ILeaf<int, TestIndexTreeLeaf>
    {
        private const int Capacity = 2;
        private readonly List<int> _storage = new List<int>(Capacity);
        private readonly int _firstValueIndex;
        public TestIndexTreeLeaf(int value, int index)
        {
            _storage.Add(value);
            _firstValueIndex = index;
        }

        public TestIndexTreeLeaf? Add(int value, int valueIndex)
        {
            if (_storage.Count >= Capacity)
            {
                Next = new TestIndexTreeLeaf(value, valueIndex);
                return Next;
            }

            _storage.Add(value);
            return null;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return _storage.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int FirstValue => _storage[0];
        public override int MinIndex => _firstValueIndex;
        public override IEnumerable<int> GetEnumerableFromIndex(int index)
        {
            return this.GetEnumerableFromIndex<int, TestIndexTreeLeaf>(index);
        }
        
        public override int GetValue(int index)
        {
            return this.GetValue<int, TestIndexTreeLeaf>(index);
        }

        public override (int index, TestIndexTreeLeaf leaf) FindByValue(int value)
        {
            var index = _storage.BinarySearch((int) (value));
            return (index, this);
        }

        public int this[int index] => _storage[index];

        public int Count => _storage.Count;

        public TestIndexTreeLeaf? Next { get; private set; }
    }
}