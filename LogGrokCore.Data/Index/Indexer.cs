using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data.IndexTree;
using NLog;

namespace LogGrokCore.Data.Index
{
    public class Indexer : IDisposable
    {
        private readonly ConcurrentDictionary<IndexKey, IndexTree<int, SimpleLeaf<int>>> _indices =
            new(1, 16384);

        private readonly ConcurrentDictionary<int, HashSet<IndexKey>> _components = new();
        
        private readonly CountIndex<IndexTree<int, SimpleLeaf<int>>> _countIndex 
            = new();

        private readonly object _componentsLocker = new();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public IEnumerable<string> GetAllComponents(int componentNumber)
        {
            var componentSet = _components[componentNumber];
            lock (_componentsLocker)
            {
                return componentSet
                    .Select(key => key.GetComponent(componentNumber).ToString());
            }
        }

        public IIndexedLinesProvider GetIndexedLinesProvider(IReadOnlyDictionary<int, IEnumerable<string>> excludedComponents)
        {
            var updatableCounts = 
                UpdatableValue.Create(() => _countIndex.Counts);
            return new IndexedLinesProvider(this, updatableCounts, 
                CountIndex<IndexTree<int, SimpleLeaf<int>>>.Granularity, excludedComponents);
        }

        public IndexTree<int, SimpleLeaf<int>> GetIndex(IndexKey key) => _indices[key];
        
        public void Add(IndexKey key, int lineNumber)
        {
            var index = _indices.GetOrAdd(key,
                static indexedKey =>
                {
                    indexedKey.MakeLocalCopy();
                    return CreateIndexTree();
                });
            
            index.Add(lineNumber);    
            _countIndex.Add(lineNumber, _indices);

            var newIndexCreated = key.HasLocalBuffer;
            if (newIndexCreated)
                UpdateComponents(key);
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
            Logger.Info($"GetIndexedSequenceFrom({from})");
            static bool IsNext(LineAndKey lk1, LineAndKey lk2)
            {
                return lk2.LineNumber == lk1.LineNumber + 1;
            }

            var cursors = _indices
                .Select(kv
                    => kv.Value.GetEnumerableFromValue(from).Select(ln => new LineAndKey(ln, kv.Key)).GetEnumerator())
                .Where(enumerator => enumerator.MoveNext()).ToList();

            var startValues = string.Join(",", cursors.Select(c => c.Current.LineNumber).OrderBy(i => i)
                .Select(j => j.ToString()));

            Logger.Debug($"Start values: {startValues}"); 
            return CollectionUtlis.MergeSorted(cursors, IsNext);
        }
    
        public Indexer CreateFilteredIndexer(IAsyncEnumerable<IEnumerable<int>> filterSequence)
        {
            var filteredIndexer = new Indexer();
            FillFilteredIndexer(this, filteredIndexer, filterSequence);
            return filteredIndexer;
        }

        private static async void FillFilteredIndexer(Indexer source, Indexer target,
            IAsyncEnumerable<IEnumerable<int>> filterSequences)
        {
            await foreach (var filterSequence in filterSequences)
            {
                using var filterEnumerator = filterSequence.GetEnumerator();
                if (!filterEnumerator.MoveNext()) continue;
                MergeAndFilterIndices(target, source._indices, filterEnumerator);
            }
        }

        private static void MergeAndFilterIndices(
            Indexer target,
            IDictionary<IndexKey, IndexTree<int, SimpleLeaf<int>>> source, 
            IEnumerator<int> filterEnumerator)
        {
            var cursors =
                source.Select(kv => kv.Value.GetEnumerableFromIndex(filterEnumerator.Current)
                        .Select(value => (kv.Key, value)).GetEnumerator())
                        .Where(c => c.MoveNext());
            
            var merged = CollectionUtlis.MergeSorted(cursors, (k1, k2) => k2.value == k1.value + 1);
            using var sourceSequenceEnumerator = merged.GetEnumerator();

            while (sourceSequenceEnumerator.MoveNext())
            {
                if (sourceSequenceEnumerator.Current.value != filterEnumerator.Current) continue;

                var (key, value) = sourceSequenceEnumerator.Current;
                target.Add(key, value);
                if (!filterEnumerator.MoveNext()) return;
            }
        }
    }
}