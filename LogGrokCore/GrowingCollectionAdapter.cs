using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace LogGrokCore
{
    class GrowingCollectionAdapter<T> : IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private IList<T> _sourceCollection;

        private int _count;

        private IList SourceList => (IList)_sourceCollection;

        public GrowingCollectionAdapter(IList<T> sourceCollection)
        {
            _sourceCollection = sourceCollection;
            _count = sourceCollection.Count;
        }

        public void UpdateCount()
        {
            if (_count == _sourceCollection.Count)
                return;

            _count = _sourceCollection.Count;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }

        public T this[int index] { get => _sourceCollection[index]; set => _sourceCollection[index] = value; }

        object? IList.this[int index] { get => _sourceCollection[index]; set => throw new NotImplementedException(); }

        public int Count => _sourceCollection.Count;

        public bool IsReadOnly => _sourceCollection.IsReadOnly;

        public bool IsFixedSize => false;

        public bool IsSynchronized => SourceList.IsSynchronized;

        public object SyncRoot => SourceList.SyncRoot;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Add(T item) => _sourceCollection.Add(item);

        public int Add(object value) => SourceList.Add(value);

        public void Clear() => _sourceCollection.Clear();

        public bool Contains(T item) => _sourceCollection.Contains(item);

        public bool Contains(object value) => SourceList.Contains(value);

        public void CopyTo(T[] array, int arrayIndex) => _sourceCollection.CopyTo(array, arrayIndex);

        public void CopyTo(Array array, int index) => SourceList.CopyTo(array, index);

        public IEnumerator<T> GetEnumerator() => _sourceCollection.GetEnumerator();

        public int IndexOf(T item) => _sourceCollection.IndexOf(item);

        public int IndexOf(object value) => SourceList.IndexOf(value);

        public void Insert(int index, T item) => _sourceCollection.Insert(index, item);

        public void Insert(int index, object value) => SourceList.Insert(index, value);

        public bool Remove(T item) => _sourceCollection.Remove(item);

        public void Remove(object value) => SourceList.Remove(value);

        public void RemoveAt(int index) => _sourceCollection.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => SourceList.GetEnumerator();
    }
}
