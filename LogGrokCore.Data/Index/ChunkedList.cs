using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Data.Index;

public class ChunkedList<T> : IList<T>
{
    private readonly List<T[]> _chunks = new();
    private readonly int _chunkSize;
    private int _count;
    
    public ChunkedList(int chunkSize)
    {
        _chunkSize = chunkSize;
    }

    private IEnumerable<T> GetEnumerableFrom(int index)
    {
        if (index >= _count)
            throw new IndexOutOfRangeException();

        var chunkNum = index / _chunkSize;
        var from = index % _chunkSize;

        while (index < _count)
        {
            var currentChunk = _chunks[chunkNum];
            var to = Math.Min(_count - chunkNum * _chunkSize, _chunkSize);
            
            for (var idx = from; idx < to; idx++)
                yield return currentChunk[idx];
            
            index += to - from;
            chunkNum++;
            from = 0;
        }
        
    }

    public IEnumerator<T> GetEnumerator() => GetEnumerableFrom(0).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(T item)
    {
        var idx = _count % _chunkSize; 
        if (_count % _chunkSize == 0)
            _chunks.Add(new T[_chunkSize]);

        var lastChunk = _chunks[^1];
        lastChunk[idx] = item;
        _count++;
    }

    public void Clear()
    {
        _chunks.Clear();
    }

    public bool Contains(T item) => throw new NotSupportedException();

    public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();

    public bool Remove(T item) => throw new NotSupportedException();

    public int Count => _count;

    public bool IsReadOnly => false;

    public int IndexOf(T item) => throw new NotSupportedException();

    public void Insert(int index, T item) => throw new NotSupportedException();

    public void RemoveAt(int index) => throw new NotSupportedException();

    public T this[int index]
    {
        get
        {
            if (index >= _count)
                throw new IndexOutOfRangeException();

            return _chunks[index / _chunkSize][index % _chunkSize];
        }
        
        set => throw new NotSupportedException();
    }
}