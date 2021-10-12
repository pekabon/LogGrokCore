using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace LogGrokCore
{
    public interface IGrowingCollection
    {
        public event Action<int> CollectionGrown;
    }

    public class GrowingLogLinesCollection: IList<ItemViewModel>, IList, INotifyCollectionChanged, INotifyPropertyChanged, IGrowingCollection
    {
        private IReadOnlyList<ItemViewModel> _sourceCollection;
        private int _logLinesCount;
        private IReadOnlyList<ItemViewModel> _headerCollection;
        private int _headerCollectionCount;

        private IList SourceList => (IList)_sourceCollection;

        public GrowingLogLinesCollection(
                IReadOnlyList<ItemViewModel> headerCollection, 
                IReadOnlyList<ItemViewModel> sourceCollection)
        {
            _sourceCollection = sourceCollection;
            _headerCollection = headerCollection;
            _logLinesCount = sourceCollection.Count;
            _headerCollectionCount = headerCollection.Count;
        }

        public void Reset(IReadOnlyList<ItemViewModel> headerCollection,
            IReadOnlyList<ItemViewModel> sourceCollection)
        {
            _sourceCollection = sourceCollection;
            _headerCollection = headerCollection;
            UpdateCount();
        }
        
        public void UpdateCount()
        {
            if (_logLinesCount == _sourceCollection.Count 
                && _headerCollectionCount == _headerCollection.Count)
                return;

            _logLinesCount= _sourceCollection.Count;
            _headerCollectionCount = _headerCollection.Count;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            CollectionGrown?.Invoke(_logLinesCount);
        }
        
        public ItemViewModel this[int index]
        {
            get => index < _headerCollectionCount ? 
                _headerCollection[index] : _sourceCollection[index - _headerCollectionCount];
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
                return _logLinesCount + _headerCollectionCount;
            }
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => false;

        public bool IsSynchronized => throw new NotSupportedException();

        public object SyncRoot =>  throw new NotSupportedException();
        
#pragma warning disable CS0067
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning restore
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Add(ItemViewModel item) => throw new NotSupportedException();

        public int Add(object? value) =>  throw new NotSupportedException();

        public void Clear() =>  throw new NotSupportedException();

        public bool Contains(ItemViewModel item) => _sourceCollection.Any(i => i == item);

        public bool Contains(object? value) => SourceList.Contains(value);

        public void CopyTo(ItemViewModel[] array, int arrayIndex) => throw new NotSupportedException();

        public void CopyTo(Array array, int index) => throw new NotSupportedException();

        public IEnumerator<ItemViewModel> GetEnumerator()
        {
            if (_logLinesCount == 0)
                yield break;

            foreach (var itemViewModel in _headerCollection)
            {
                yield return itemViewModel;
            }

            foreach (var itemViewModel in _sourceCollection)
            {
                yield return itemViewModel;
            }
        }

        public int IndexOf(ItemViewModel item) => throw new NotSupportedException();

        public int IndexOf(object? value) => SourceList.IndexOf(value); 

        public void Insert(int index, ItemViewModel item) => throw new NotSupportedException();

        public void Insert(int index, object? value) => throw new NotSupportedException();

        public bool Remove(ItemViewModel item) => throw new NotSupportedException();

        public void Remove(object? value) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public event Action<int>? CollectionGrown;
    }
}
