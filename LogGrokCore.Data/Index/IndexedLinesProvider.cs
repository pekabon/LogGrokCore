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
        private readonly FilteredCountIndicesProvider<IndexKey> _filteredCountIndicesProvider;

        public IndexedLinesProvider(
            Indexer indexer, 
            IReadOnlyList<List<(IndexKey, int)>> countIndices,
            int countIndexGranularity,
            Dictionary<int, List<string>> excludedComponents)
        {
            _indexer = indexer;

            bool IsKeyExcluded(IndexKey key)
            {
#pragma warning disable CS8619
                foreach (var (componentIndex, componentValues) in excludedComponents)
#pragma warning restore CS8619
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
            _filteredCountIndicesProvider =
                new FilteredCountIndicesProvider<IndexKey>(
                    isKeyIncluded, countIndices,
                    countIndexGranularity);
        }

        public int Count => _filteredCountIndicesProvider.Count;
        
        public void Fetch(int start, Span<int> values)
        {
            
            var idx = 0;
            foreach (var lineNumber in GetEnumerableFrom(start).Take(values.Length))
            {
                values[idx++] = lineNumber;
            }
        }

        private IEnumerable<int> GetEnumerableFrom(int start)
        {
            var countIndicesItem = _filteredCountIndicesProvider.GetStartIndices(start);
            var startCount = countIndicesItem.TotalCount;

            var startIndices = countIndicesItem.Counts.ToDictionary(item => item.key, item => item.count);
            var indices = _filteredCountIndicesProvider.GetAllKeys().Select(key =>
            {
                var index = _indexer.GetIndex(key);
                return index.EnumerateFrom(startIndices.TryGetValue(key, out var startPosition) ? startPosition : 0);
            });

            return CollectionUtlis.MergeSorted(indices).Skip(start - startCount);
        }
    }
}