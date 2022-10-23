using System;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Data.Index
{
    internal class IndexedLinesProvider : IIndexedLinesProvider
    {
        private readonly IndexerBase _indexer;
        private readonly FilteredCountIndicesProvider<IndexKeyNum> _filteredCountIndicesProvider;

        public IndexedLinesProvider(
            IndexerBase indexer, 
            UpdatableValue<IReadOnlyList<List<(IndexKeyNum, int)>>> countIndices,
            int countIndexGranularity,
            IReadOnlyDictionary<int, IEnumerable<string>> excludedComponents,
            IReadOnlyDictionary<IndexKeyNum, IndexKey> numbersToKeys)
        {
            _indexer = indexer;

            bool IsKeyExcluded(IndexKeyNum keyNum)
            {
#pragma warning disable CS8619
                foreach (var (componentIndex, componentValues) in excludedComponents)
#pragma warning restore CS8619
                {
                    var keyComponent = numbersToKeys[keyNum].GetComponent(componentIndex);
                    foreach (var componentValue in componentValues)
                    {
                        if (keyComponent.SequenceEqual(componentValue))
                            return true;
                    }
                }

                return false;
            }

            Predicate<IndexKeyNum> isKeyIncluded = key => !IsKeyExcluded(key);
            _filteredCountIndicesProvider =
                new FilteredCountIndicesProvider<IndexKeyNum>(
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

        public int GetIndexByValue(int value)
        {
            var countIndicesItem = _filteredCountIndicesProvider.GetStartIndicesForValue(value);
            var index = countIndicesItem.TotalCount;
            foreach (var v in GetEnumerable(countIndicesItem))
            {
                if (v >= value) return index;
                index++;
            }

            return Count - 1;
        }

        private IEnumerable<int> GetEnumerable(CountIndexItem<IndexKeyNum> countIndicesItem)
        {
            var startIndices = 
                countIndicesItem.Counts.ToDictionary(item => item.key, 
                    item => item.count);

            var indices = _filteredCountIndicesProvider.GetAllKeys().Select(key =>
            {
                var index = _indexer.GetIndex(key);
               
                return index.GetEnumerableFromIndex(
                    startIndices.TryGetValue(key, out var startPosition) 
                        ? startPosition 
                        : 0); 
            });

            return CollectionUtils.MergeSorted(indices);
        }

        private IEnumerable<int> GetEnumerableFrom(int start)
        {
            var countIndicesItem = _filteredCountIndicesProvider.GetStartIndices(start);
            var startCount = countIndicesItem.TotalCount;
            return GetEnumerable(countIndicesItem).Skip(start - startCount);
        }
    }
}