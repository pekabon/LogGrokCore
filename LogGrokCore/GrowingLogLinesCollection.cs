using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LogGrokCore
{
    public interface IGrowingCollection
    {
        public event Action<int> CollectionGrown;
    }

    public class GrowingLogLinesCollection: IList<ItemViewModel>, IList, INotifyCollectionChanged, INotifyPropertyChanged, IGrowingCollection
    {
        private readonly IList<ItemViewModel> _sourceCollection;
        private int _logLinesCount;
        private readonly Lazy<string?> _header;
        
        private readonly Lazy<LogHeaderViewModel?> _headerViewModel;
        
        private IList SourceList => (IList)_sourceCollection;

        public GrowingLogLinesCollection(Func<string?> headerProvider, IList<ItemViewModel> sourceCollection)
        {
            _sourceCollection = sourceCollection;
            
            _header = new Lazy<string?>(headerProvider);
            
            _headerViewModel = new Lazy<LogHeaderViewModel?>(
                () => _header.Value == null ? null : new LogHeaderViewModel(_header.Value));
            
            _logLinesCount = sourceCollection.Count;
          
        }
        
        public void UpdateCount()
        {
            if (_logLinesCount == _sourceCollection.Count)
                return;

            _logLinesCount= _sourceCollection.Count;
            //CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionGrown?.Invoke(_logLinesCount);
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
                if (_logLinesCount  == 0)
                    return 0;
                return _logLinesCount + HeaderCount;
            }
        }

        public bool IsReadOnly => _sourceCollection.IsReadOnly;

        public bool IsFixedSize => false;

        public bool IsSynchronized => SourceList.IsSynchronized;

        public object SyncRoot => SourceList.SyncRoot;
        
#pragma warning disable CS0067
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning restore
        
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
            
            if (_header.Value != null)
                yield return new LogHeaderViewModel(_header.Value);

            foreach (var itemViewModel in _sourceCollection)
            {
                yield return itemViewModel;
            }
        }

        public int IndexOf(ItemViewModel item) => _sourceCollection.IndexOf(item) + HeaderCount;

        public int IndexOf(object? value) => SourceList.IndexOf(value) + HeaderCount;

        public void Insert(int index, ItemViewModel item) => throw new NotSupportedException();

        public void Insert(int index, object? value) => throw new NotSupportedException();

        public bool Remove(ItemViewModel item) => throw new NotSupportedException();

        public void Remove(object? value) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        private int HeaderCount => _header.Value != null ? 1 : 0; 
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public event Action<int>? CollectionGrown;
    }
}
