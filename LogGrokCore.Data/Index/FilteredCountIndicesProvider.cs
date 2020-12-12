using System;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Data.Index
{
    internal class FilteredCountIndicesProvider<T>
    {
        private readonly UpdatableValue<ListLazyAdapter<List<(T, int)>, CountIndexItem<T>>> _filteredCountIndices;
        private readonly int _granularity;

        public FilteredCountIndicesProvider(Predicate<T> isIncluded,
            UpdatableValue<IReadOnlyList<List<(T, int)>>> countIndices,
            int granularity)
        {
            CountIndexItem<T> CreateIndexItem(List<(T, int)> item)
            {
                var filtered = new List<(T, int)>(item.Count);
                filtered.AddRange(item.Where(t => isIncluded(t.Item1)));
                return new CountIndexItem<T>(filtered);
            }

            _filteredCountIndices = countIndices
            .Map(c =>
            new ListLazyAdapter<List<(T, int)>, CountIndexItem<T>>(c, CreateIndexItem));
            _granularity = granularity;
        }

        public int Count
        {
            get
            {
                var filteredCountIndices = _filteredCountIndices.Value;
                return filteredCountIndices.Count == 0 ? 0 : filteredCountIndices[^1].TotalCount;
            }
        }

        public IEnumerable<T> GetAllKeys()
        {
            var countsIndexList = _filteredCountIndices.Value;
            return countsIndexList.Count == 0 ? Enumerable.Empty<T>() : countsIndexList[^1].Counts.Select(t => t.key);
        }

        public CountIndexItem<T> GetStartIndicesForValue(int value)
        {
            var idx = (value + 1) / _granularity;
            return idx == 0 ? CountIndexItem<T>.Empty : _filteredCountIndices.Value[idx - 1];
        }

        public CountIndexItem<T> GetStartIndices(int position)
        {
            var filteredCountIndices = _filteredCountIndices.Value;
            var found = filteredCountIndices.BinarySearch(position, 
                (element, p) => element.TotalCount.CompareTo(p));
            
            var  foundIdx =  (found < 0) ? ~found  : found;
            return foundIdx == 0 ? CountIndexItem<T>.Empty : filteredCountIndices[foundIdx - 1];
        }
    }
}