using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LogGrokCore.Data.IndexTree;

namespace LogGrokCore.Data.Index
{
    public class Indexer : IDisposable
    {
        private readonly ConcurrentDictionary<IndexKey, IndexTree<int, SimpleLeaf<int>>> _indices =
            new(1, 16384);

        private readonly Func<IndexKey, IndexTree<int, SimpleLeaf<int>>> _indexValueFactory;

        private readonly ConcurrentDictionary<int, HashSet<IndexKey>> _components = new();

        private readonly CountIndex<IndexTree<int, SimpleLeaf<int>>> _countIndex;

        private readonly object _componentsLocker = new();

        private readonly Dictionary<IndexKey, IndexKey> _keyDictionary = new();

        private readonly IndexTree<LineAndKey, SimpleLeaf<LineAndKey>> _lineAndKeyIndex;

        public Indexer()
        {
            _countIndex = new CountIndex<IndexTree<int, SimpleLeaf<int>>>(_indices);
            _lineAndKeyIndex =
                new IndexTree<LineAndKey, SimpleLeaf<LineAndKey>>(16384,
                    static value => new SimpleLeaf<LineAndKey>(value, 0));
            _indexValueFactory = indexedKey =>
            {
                indexedKey.MakeLocalCopy();
                _keyDictionary[indexedKey] = indexedKey;
                return CreateIndexTree();
            };
        }

        public IEnumerable<string> GetAllComponents(int componentNumber)
        {
            if (!_components.TryGetValue(componentNumber, out var componentSet))
                return Enumerable.Empty<string>();

            lock (_componentsLocker)
            {
                return componentSet
                    .Select(key => key.GetComponent(componentNumber).ToString()).ToList();
            }
        }

        public IIndexedLinesProvider GetIndexedLinesProvider(
            IReadOnlyDictionary<int, IEnumerable<string>> excludedComponents)
        {
            var updatableCounts =
                UpdatableValue.Create(() => _countIndex.Counts);
            return new IndexedLinesProvider(this, updatableCounts,
                CountIndex<IndexTree<int, SimpleLeaf<int>>>.Granularity, excludedComponents);
        }

        public IndexTree<int, SimpleLeaf<int>> GetIndex(IndexKey key) => _indices[key];


        public void Add(IndexKey key, int lineNumber)
        {
            var index = _indices.GetOrAdd(key, _indexValueFactory);
            
            index.Add(lineNumber);
            _countIndex.Add(lineNumber, _indices);

            var newIndexCreated = key.HasLocalBuffer;
            if (newIndexCreated)
                UpdateComponents(key);

            if (!_keyDictionary.TryGetValue(key, out var localKey))
            {
                key.MakeLocalCopy();
                _keyDictionary[key] = key;
                localKey = key;
            }

            _lineAndKeyIndex.Add(new LineAndKey(lineNumber, localKey));
        }

        public event Action<(int compnentNumber, IndexKey key)>? NewComponentAdded;

        public int GetIndexCountForComponent(int componentIndex, string componentValue)
        {
            return _indices
                .Where(keyValuePair =>
                    keyValuePair.Key.GetComponent(componentIndex).SequenceEqual(componentValue.AsSpan()))
                .Sum(kv => kv.Value.Count);
        }

        private class ComponentComparer : IEqualityComparer<IndexKey>
        {
            private readonly int _index;

            public ComponentComparer(int index) => _index = index;

            public bool Equals(IndexKey? x, IndexKey? y) =>
                x != null && y != null &&
                x.GetComponent(_index).SequenceEqual(y.GetComponent(_index));

            public int GetHashCode(IndexKey obj) => string.GetHashCode(obj.GetComponent(_index));
        }

        private void UpdateComponents(IndexKey key)
        {
            for (var componentIndex = 0; componentIndex < key.ComponentCount; componentIndex++)
            {
                var componentSet = _components.GetOrAdd(componentIndex,
                    static index => new HashSet<IndexKey>(new ComponentComparer(index)));

                bool isAdded;
                lock (_componentsLocker)
                {
                    isAdded = componentSet.Add(key);
                }

                if (isAdded)
                    NewComponentAdded?.Invoke((componentIndex, key));
            }
        }

        private static IndexTree<int, SimpleLeaf<int>> CreateIndexTree()
        {
            return new(16,
                static value => new SimpleLeaf<int>(value, 0));
        }

        public void Dispose()
        {
            _indices.Clear();
        }

        public void Finish()
        {
            _countIndex.Finish(_indices);
        }

        public readonly struct LineAndKey : IComparable<LineAndKey>
        {
            public readonly int LineNumber;
            public readonly IndexKey Key;

            public LineAndKey(int lineNumber, IndexKey key)
            {
                LineNumber = lineNumber;
                Key = key;
            }

            public int CompareTo(LineAndKey other)
            {
                return LineNumber.CompareTo(other.LineNumber);
            }

            public void Deconstruct(out int lineAndNumber, out IndexKey indexKey)
            {
                lineAndNumber = LineNumber;
                indexKey = Key;
            }

        }

        public IEnumerable<LineAndKey> GetIndexedSequenceFrom(int from)
        {
            Trace.TraceInformation($"GetIndexedSequenceFrom({from})");
            return _lineAndKeyIndex.GetEnumerableFromIndex(from);
        }
    }
}