using System;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Data.Index
{
    internal class CountIndexItem<T>
    {
        public CountIndexItem(IReadOnlyList<(T, int)> counts)
        {
            Counts = counts;
            _totalCount = new Lazy<int>(() => Counts.Sum(item => item.Item2));
        }

        public IReadOnlyList<(T key, int count)> Counts { get; }

        public int TotalCount => _totalCount.Value;

        public static CountIndexItem<T> Empty { get; } = new CountIndexItem<T>(new List<(T, int)>(0));

        private readonly Lazy<int> _totalCount;
    }
}