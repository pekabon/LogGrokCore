using System;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Data.Index
{
    internal class FilteredCountIndicesProvider<T>
    {
        private readonly ListLazyAdapter<List<(T, int)>, CountIndexItem<T>> _filteredCountIndices;
        private readonly int _granularity;

        public FilteredCountIndicesProvider(Predicate<T> isIncluded,
            IReadOnlyList<List<(T, int)>> countIndices,
            int granularity)
        {
            CountIndexItem<T> CreateIndexItem(List<(T, int)> item)
            {
                var filtered = new List<(T, int)>(item.Count);
                filtered.AddRange(item.Where(t => isIncluded(t.Item1)));
                return new CountIndexItem<T>(filtered);
            }

            _filteredCountIndices = 
                new ListLazyAdapter<List<(T, int)>, CountIndexItem<T>>(countIndices, CreateIndexItem);
            _granularity = granularity;
        }

        public int Count => _filteredCountIndices.Select(t=> t.TotalCount).LastOrDefault();

        public IEnumerable<T> GetAllKeys()
        {
            return _filteredCountIndices.LastOrDefault()?.Counts?.Select(t => t.Item1) ?? Enumerable.Empty<T>();
        }

        public  CountIndexItem<T> GetStartIndicesForValue(int value)
        {
            var idx = (value + 1) / _granularity;
            return idx == 0 ? CountIndexItem<T>.Empty : _filteredCountIndices[idx - 1];
        }

        public CountIndexItem<T> GetStartIndices(int position)
        {
            var found = _filteredCountIndices.BinarySearch(position, (leaf, p) => leaf.TotalCount.CompareTo(p));
            var  foundIdx =  (found < 0) ? ~found  : found;
            return foundIdx == 0 ? CountIndexItem<T>.Empty : _filteredCountIndices[foundIdx - 1];
        }
    }
}