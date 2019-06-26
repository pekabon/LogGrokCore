using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#nullable enable

namespace LogGrokCore.Data.Virtualization
{
    public class VirtualList<TSource, T> : IList<T>, IList
	{
        private struct PageItem
        {
            public T Value
            {
                get
                {
                    if (EqualityComparer<T>.Default.Equals(_value, default(T)))
                        _value = _converter(_source);
                    return _value;
                }
            }

            private T _value;
            
            private readonly TSource _source;

            private Converter<TSource, T> _converter;
            
            
            public PageItem(TSource source, Converter<TSource, T> converter)
            {
                _source = source;
                _converter = converter;
                _value = default;
            }
        }

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
            get
            {
                var (pageIndex, page) = QueryPage(index);
                return page[pageIndex].Value;
            }

            set => throw new NotSupportedException();
        }

        public VirtualList(IItemProvider<TSource> itemProvider, Converter<TSource, T> converter)
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

        public int Add(object value) => throw new NotSupportedException();

        public bool Contains(object value) => throw new NotSupportedException();

        public int IndexOf(object value) => 0;
        
        public void Insert(int index, object value) => throw new NotSupportedException();

        public void Remove(object value) => throw new NotSupportedException();

        public void CopyTo(Array array, int index) => throw new NotSupportedException();

        private (int indexOnPage, IList<PageItem> page) QueryPage(int index) 
        {
            var pageIndex = index / PageSize;
            var pageStart = pageIndex * PageSize;

            if (_pageCache.TryGetValue(pageIndex, out var pageWithGeneration))
            {
                var (page, _) = pageWithGeneration;
                if (index - pageStart < page.Count) return (index - pageStart, page);
                
                var newLines = _itemProvider
                    .Fetch(pageStart + page.Count, Math.Min(PageSize, Count - pageStart));
                foreach (var line in newLines)
                    page.Add(new PageItem(line, _converter));

                return (index - pageStart, page);
            }

            var data = _itemProvider.Fetch(pageStart, Math.Min(PageSize, Count - pageStart));

            var tempPage = new List<PageItem>(PageSize);
            Convert(data, tempPage);

            _pageCache[pageIndex] = (tempPage, ++ _pageCounter);
            CleanupCache();
            return (index - pageStart, tempPage);
        }

        private void Convert(IList<TSource> source, List<PageItem> items)
        {
            for (var i = 0; i < source.Count; i++)
            {
                items.Add(new PageItem(source[i], _converter));
            }
        }

        private void CleanupCache() 
        {
        	while (_pageCache.Count > MaxCacheSize)
        	{
                var oldestKey = _pageCache.OrderBy(pair => pair.Value.Item2).First().Key;
                _ = _pageCache.Remove(oldestKey);
            }
        }

        private const int PageSize = 100;
        private const int MaxCacheSize = 10;
        private readonly Converter<TSource, T> _converter;
        private readonly Dictionary<int, (IList<PageItem>, int)> _pageCache = new Dictionary<int, (IList<PageItem>, int)>();
        private int _pageCounter;
    }
}
