using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data.Index
{
    internal class IndexedLinesProvider : IItemProvider<int>
    {
        private readonly Indexer _indexer;
        private readonly FilteredCountIndicesProvider<IndexKey> _countsIndexer;

        public IndexedLinesProvider(
            Indexer indexer, 
            IReadOnlyList<List<(IndexKey, int)>> countIndices,
            int countIndexGranularity,
            Dictionary<int, List<string>> excludedComponents)
        {
            _indexer = indexer;

            bool IsKeyExcluded(IndexKey key)
            {
                foreach (var (componentIndex, componentValues) in excludedComponents)
                {
                    var keyComponent = key.GetComponent(componentIndex);
                    foreach (var componentValue in componentValues)
                    {
                        if (keyComponent.SequenceEqual(componentValue))
                            return true;
                    }
                }

                return false;
            }

            Predicate<IndexKey> isKeyIncluded = key => !IsKeyExcluded(key);
            _countsIndexer =
                new FilteredCountIndicesProvider<IndexKey>(
                    isKeyIncluded, countIndices,
                    countIndexGranularity);
        }

        public int Count => _countsIndexer.Count;
        
        public void Fetch(int start, Span<int> values)
        {
            var idx = 0;
            foreach (var lineNumber in GetEnumerableFrom(start))
            {
                values[idx++] = lineNumber;
            }
        }

        private IEnumerable<int> GetEnumerableFrom(int start)
        {
            var countIndicesItem = _countsIndexer.GetStartIndices(start);
            var startCount = countIndicesItem.TotalCount;

            var startIndices = countIndicesItem.Counts.ToDictionary(item => item.key, item => item.count);
            var indices = _countsIndexer.GetAllKeys().Select(key =>
            {
                var index = _indexer.GetIndex(key);
                return index.EnumerateFrom(startIndices.TryGetValue(key, out var startPosition) ? startPosition : 0);
            });

            return CollectionUtlis.MergeSorted(indices).Skip(start - startCount);
        }
    }
}