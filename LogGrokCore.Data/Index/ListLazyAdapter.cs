using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace LogGrokCore.Data.Index
{
    internal class ListLazyAdapter<TSource, TTarget> : IReadOnlyList<TTarget> where TTarget : class
    {
        private TTarget[] _converted;
        private readonly IReadOnlyList<TSource> _source;
        private readonly Converter<TSource, TTarget> _converter;
        private readonly ReaderWriterLockSlim _lock;
        
        public ListLazyAdapter(IReadOnlyList<TSource> source, Converter<TSource, TTarget> converter)
        {
            _source = source;
            _converter = converter;
            _converted = new TTarget[source.Count];
            _lock = new ReaderWriterLockSlim();
        }

        public IEnumerator<TTarget> GetEnumerator()
        {
            for (var idx = 0; idx < Count; idx++)
                yield return this[idx];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TTarget this[int index]
        {
            get
            {
                using var upgradableReadLockOwner = _lock.GetUpgradableReadLockOwner();
                if (index >= _converted.Length)
                {
                    using var writeLockOwner = _lock.GetWriteLockOwner();
                    if (index >= _converted.Length)
                    {
                        Array.Resize(ref _converted, _source.Count);
                    }
                }
            
                var alreadyConverted = _converted[index];
                if (alreadyConverted != null) return alreadyConverted;

                using var writeLockOwner2 = _lock.GetWriteLockOwner();
                var converted = _converter(_source[index]);
                _converted[index] = converted;
                return converted;
            }
            set => throw new NotSupportedException();
        }
        
        public void Add(TTarget item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TTarget item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(TTarget[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(TTarget item)
        {
            throw new NotSupportedException();
        }

        public int Count => _source.Count;

        public bool IsReadOnly => true;
        
        public int IndexOf(TTarget item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, TTarget item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }
}