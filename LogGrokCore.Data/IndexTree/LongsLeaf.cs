using System.Collections;
using System.Collections.Generic;

namespace LogGrokCore.Data.IndexTree
{
    public class LongsLeaf
        : LeafOrNode<long, LongsLeaf>,
            ILeaf<long, LongsLeaf>
    {
        private const int Capacity = 64*1024;
        private readonly long _firstValue;
        private readonly int _firstIndex;
        private readonly List<int> _storage;
        
        public LongsLeaf(long firstValue, int valueIndex)
        {
            _storage = new List<int>(Capacity) {0};
            _firstIndex = valueIndex;
            _firstValue = firstValue;
        }

        public LongsLeaf? Add(long value, int valueIndex)
        {
            if (_storage.Count < Capacity)
            {
                _storage.Add((int)(value - _firstValue));
                return null;
            }

            Next = new LongsLeaf(value, valueIndex);
            return Next;
        }

        public long this[int index] => _firstValue +_storage[index];

        public int Count => _storage.Count;
        public LongsLeaf? Next { get; private set; }
        
        public override long FirstValue => _firstValue;
        public override int MinIndex => _firstIndex;
        
        public override IEnumerable<long> GetEnumerableFromIndex(int index)
        {
            return this.GetEnumerableFromIndex<long, LongsLeaf>(index);
        }

        public override long GetValue(int index)
        {
            return this.GetValue<long, LongsLeaf>(index);
        }

        public override (int index, LongsLeaf leaf) FindByValue(long value)
        {
            var index = _storage.BinarySearch((int) (value - _firstIndex));
            return (index, this);
        }

        public IEnumerator<long> GetEnumerator()
        {
            foreach (var value in _storage)
            {
                yield return _firstIndex + value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}