 using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

 namespace LogGrokCore.Data.Virtualization
{
    public class VirtualList<TSource, T> : IList<T>, IReadOnlyList<T>, IList where T : class
	{
        private readonly IItemProvider<TSource> _itemProvider;

        public int Count => _itemProvider.Count;

        public bool IsReadOnly => true;

        public bool IsFixedSize => false;

        public bool IsSynchronized => false;

        public object SyncRoot => throw new NotSupportedException();

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        public T this[int index]
        {
            get => GetValue(index);
            set => throw new NotSupportedException();
        }

        public VirtualList(IItemProvider<TSource> itemProvider, Func<TSource, T> converter)
        {
            _itemProvider = itemProvider;
            _converter = converter;
        }

        public IEnumerator<T> GetEnumerator() 
		{
            for(var index = 0; index < Count; index++)
            {
                yield return this[index];
            }
		}

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Remove(T item) => throw new NotSupportedException();

        public int IndexOf(T item) => throw new NotSupportedException();

        public void Insert(int index, T item) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        public void Add(T item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(T item) => throw new NotSupportedException();

        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();

        public int Add(object? value) => throw new NotSupportedException();

        public bool Contains(object? value) => false;

        public int IndexOf(object? value) => 0;
        
        public void Insert(int index, object? value) => throw new NotSupportedException();

        public void Remove(object? value) => throw new NotSupportedException();

        public void CopyTo(Array array, int index) => throw new NotSupportedException();

        private T GetValue(int index) 
        {
            var pageIndex = index / PageSize;
            var pageStart = pageIndex * PageSize;

            if (_pageCache.TryGetValue(pageIndex, out var pageWithGeneration))
            {
                var (page, _) = pageWithGeneration;
                if (index - pageStart < page.Count) 
                    return page[index - pageStart];

                var count = Math.Min(PageSize, Count - pageStart) - page.Count;
                FetchAndConvert(pageStart + page.Count, count, page);
                return page[index - pageStart];
            }

            var tempPage = new PooledList<T>(PageSize);

            FetchAndConvert(pageStart, Math.Min(PageSize, Count - pageStart), tempPage);
            _pageCache[pageIndex] = (tempPage, ++ _pageCounter);
            CleanupCache();
            return tempPage[index - pageStart];
            
            void FetchAndConvert(int start, int count, PooledList<T> pageToAdd)
            {
                var arrayPool = ArrayPool<TSource>.Shared;
                var newLines = arrayPool.Rent(count);
                try
                {
                    _itemProvider.Fetch(start, newLines.AsSpan(0, count));
                    for (var idx = 0; idx < count; idx++)
                    {
                        var sourceString = newLines[idx];
                        pageToAdd.Add(_converter(sourceString)); 
                    }
                }
                finally
                {
                    arrayPool.Return(newLines, true);
                }
            }
        }

        private void CleanupCache() 
        {
        	while (_pageCache.Count > MaxCacheSize)
        	{
                var oldestKey = _pageCache.OrderBy(pair => pair.Value.Item2).First().Key;
                var (pageToReturn, _) = _pageCache[oldestKey];
                _ = _pageCache.Remove(oldestKey);
                pageToReturn.Dispose();
            }
        }

        private const int PageSize = 128;
        private const int MaxCacheSize = 10;
        private readonly Func<TSource, T> _converter;
        private readonly Dictionary<int, (PooledList<T>, int genearation)> _pageCache = new();
        private int _pageCounter;
    }
}
