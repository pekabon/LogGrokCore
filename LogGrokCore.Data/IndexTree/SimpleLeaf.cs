using System.Collections;
using System.Collections.Generic;

namespace LogGrokCore.Data.IndexTree
{
    public sealed class SimpleLeaf<T> :
        LeafOrNode<T, SimpleLeaf<T>>,
        ILeaf<T, SimpleLeaf<T>>, ITreeNode<T>
    {
        private readonly List<T> _storage;
        private readonly int _firstValueIndex;
        private const int LeafCapacity = 1024;

        public SimpleLeaf(T firstValue, int valueIndex)
        {
            _storage = new List<T>(LeafCapacity) {firstValue};
            _firstValueIndex = valueIndex;
        }

        public SimpleLeaf<T>? Add(T value, int valueIndex)
        {
            if (_storage.Count < LeafCapacity)
            {
                _storage.Add(value);
                return null;
            }

            Next = new SimpleLeaf<T>(value, valueIndex);
            return Next;
        }

        public override T FirstValue => _storage[0];

        public override int MinIndex => _firstValueIndex;

        public T this[int index] => _storage[index];

        public int Count => _storage.Count;

        public override IEnumerable<T> GetEnumerableFromIndex(int index)
        {
            return this.GetEnumerableFromIndex<T, SimpleLeaf<T>>(index);
        }
        
        public override T GetValue(int index)
        {
            return this.GetValue<T, SimpleLeaf<T>>(index);
        }

        public override (int index, SimpleLeaf<T> leaf) FindByValue(T value)
        {
            var index = _storage.BinarySearch(value);
            return ((index >= 0 ? index : ~index) + _firstValueIndex, this);
        }

        public SimpleLeaf<T>? Next { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            return _storage.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}