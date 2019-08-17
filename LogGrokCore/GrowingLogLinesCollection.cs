using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public class GrowingLogLinesCollection: IList<ItemViewModel>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private IList<ItemViewModel> _sourceCollection;

        private int _logLinesCount;
        private readonly HeaderProvider _headerProvider;
        private readonly Lazy<LogHeaderViewModel?> _headerViewModel;
        private int _headerCount;
        private IList SourceList => (IList)_sourceCollection;

        public GrowingLogLinesCollection(HeaderProvider headerProvider, IList<ItemViewModel> sourceCollection)
        {
            _sourceCollection = sourceCollection;
            _headerProvider = headerProvider;
            _headerViewModel = new Lazy<LogHeaderViewModel?>(() => 
                _headerProvider.Header == null ?  null : new LogHeaderViewModel(_headerProvider.Header));
            _logLinesCount = sourceCollection.Count;
        }

        public void UpdateCount()
        {
            if (_logLinesCount == _sourceCollection.Count)
                return;

            _headerCount = _headerProvider.Header != null ? 1 : 0;
            _logLinesCount= _sourceCollection.Count;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        public ItemViewModel this[int index]
        {
            get
            {
                if (_headerViewModel.Value == null)
                    return _sourceCollection[index];
                
                return index == 0 ? _headerViewModel.Value : _sourceCollection[index - 1];
            }
            
            set => throw new NotSupportedException();
        }

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        public int Count 
        {
            get
            {
                if (_sourceCollection.Count == 0)
                    return 0;
                return _sourceCollection.Count + _headerCount;
            }
        }

        public bool IsReadOnly => _sourceCollection.IsReadOnly;

        public bool IsFixedSize => false;

        public bool IsSynchronized => SourceList.IsSynchronized;

        public object SyncRoot => SourceList.SyncRoot;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Add(ItemViewModel item) => _sourceCollection.Add(item);

        public int Add(object? value) => SourceList.Add(value);

        public void Clear() => _sourceCollection.Clear();

        public bool Contains(ItemViewModel item) => _sourceCollection.Contains(item);

        public bool Contains(object? value) => SourceList.Contains(value);

        public void CopyTo(ItemViewModel[] array, int arrayIndex) => throw new NotSupportedException();

        public void CopyTo(Array array, int index) => throw new NotSupportedException();

        public IEnumerator<ItemViewModel> GetEnumerator()
        {
            if (_logLinesCount == 0)
                yield break;
            
            if (_headerProvider.Header != null)
                yield return new LogHeaderViewModel(_headerProvider.Header);

            foreach (var itemViewModel in _sourceCollection)
            {
                yield return itemViewModel;
            }
        }

        public int IndexOf(ItemViewModel item) => _sourceCollection.IndexOf(item) + _headerCount;

        public int IndexOf(object? value) => SourceList.IndexOf(value) + _headerCount;

        public void Insert(int index, ItemViewModel item) => throw new NotSupportedException();

        public void Insert(int index, object? value) => throw new NotSupportedException();

        public bool Remove(ItemViewModel item) => throw new NotSupportedException();

        public void Remove(object? value) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
