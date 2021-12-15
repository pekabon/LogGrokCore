using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Toolkit.HighPerformance;

namespace LogGrokCore.Data
{
    public class PooledList<T> : IList<T>, IList, IDisposable 
    {
        private T[] _data;
        private int _count;
        private const int DefaultCapacity = 128;

        public PooledList() : this(DefaultCapacity)
        {
        }
        
        public PooledList(int capacity)
        {
            _data = ArrayPool<T>.Shared.Rent(capacity);
            _count = 0;
        }

        public void Add(T value)
        {
            if (_count >= _data.Length)
            {
                Grow();
            }

            _data[_count] = value;
            _count++;
        }

        private void Grow()
        {
            var data = _data;
            ArrayPool<T>.Shared.Resize(ref data, _data.Length * 2, true);
            if (data != null)
                _data = data;
        }

        public int Add(object? value)
        {
            if (value is T t)
            {
                Add(t);
                return _count - 1;
            }

            throw new NotSupportedException();
        }

        public void Clear() => _count = 0;
        public bool Contains(object? value) => (value is T t) ? Contains(t) : throw new NotSupportedException();
            
        public int IndexOf(object? value) => (value is T t) ? IndexOf(t) : throw new NotSupportedException(); 

        public void Insert(int index, object? value) =>  throw new NotSupportedException();
        public void Remove(object? value)  => throw new NotSupportedException();  
            
        public bool Contains(T item) => IndexOf(item) > 0;
        public void CopyTo(T[] array, int arrayIndex)  => _data[.._count].CopyTo(array, arrayIndex);
        public bool Remove(T item) => throw new NotSupportedException();
        public void CopyTo(Array array, int index) => throw new NotSupportedException();
        public int Count => _count;
        public bool IsSynchronized => false;
            
        public object SyncRoot { get; } = new object();
        public bool IsReadOnly => false;

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        public int IndexOf(T? item)
        {
            for (var i = 0; i < _count; i++)
            {
                var value = _data[i];
                if (value != null)
                {
                    if (value.Equals(item))
                        return i;
                    continue;
                }

                if (item == null)
                    return i;
            }

            return - 1;
        }
        public void Insert(int index, T item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
            
        public bool IsFixedSize => false;

        public T this[int index] 
        {
            get => _data[index];
            set => _data[index] = value;
                
        } 
        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(_data);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
            {
                yield return _data[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}