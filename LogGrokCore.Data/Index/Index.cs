using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace LogGrokCore.Data.Index
{
    public class Index : IDisposable
    {
        private const int DefaultStartChunkSize = 1024;
        private int _chunkSize;
        private int[]? _currentChunk;
        private int _currentIndexInChunk;
        
        private readonly ReaderWriterLockSlim _chunksLock = new ReaderWriterLockSlim();
        
        private readonly List<(int, int[])> _chunks = new List<(int, int[])>(16);

        public Index(int chunkSize)
        {
            Debug.Assert(chunkSize > 0);
            _chunkSize = chunkSize;
        }

        public Index() : this(DefaultStartChunkSize)
        {
        }

        public void Add(int value)
        {
            if (_currentChunk == null || _currentIndexInChunk + 1 > _currentChunk.Length)
            {
                CreateNextChunk(value);
            }

            _currentChunk![_currentIndexInChunk] = value;
            _currentIndexInChunk++;
        }

        
        public IEnumerable<int> EnumerateFrom(int from)
        {
            int chunksCount;
            int[] lastChunk;
            int lastChunkCount;
            int lastChunkMaxValue;
            try
            {
                _chunksLock.EnterReadLock();
                if (_currentChunk == null)
                    yield break;
                
                chunksCount = _chunks.Count;
                lastChunk = _currentChunk;
                lastChunkCount = _currentIndexInChunk + 1;
                lastChunkMaxValue = lastChunk[_currentIndexInChunk];
            }
            finally
            {
                _chunksLock.ExitReadLock();
            }

            var (foundChunkIndex, foundIndex) =
                FindStart(from, _chunks, chunksCount, lastChunk, 
                    lastChunkMaxValue, lastChunkCount);

            var chunkIndex = foundChunkIndex;
            var chunkStartIndex = foundIndex;
            while (chunkIndex < chunksCount)
            {
                var (_, chunk) = _chunks[chunkIndex];
                for (var idx = chunkStartIndex; idx < chunk.Length; idx++)
                    yield return chunk[idx];
                chunkIndex++;
                chunkStartIndex = 0;
            }

            var startIndex = foundChunkIndex == chunksCount ? foundIndex : 0;
            for (var idx = startIndex; idx < lastChunkCount; idx++)
                yield return lastChunk[idx];
        }
        
        // Find index of smallest stored value which greater or equals then argument
        private static (int chunk, int index) FindStart(int value,
            List<(int, int[])> chunks,
            int chunksCount,
            int [] currentChunk,
            int currentChunkMaxValue,
            int currentIndexInChunk)
        {
            int GetChunkIndex(int val)
            {
                for (var i = 0; i < chunksCount; i++)
                {
                    var (key, _) = chunks[i];
                    if (key >= val)
                        return i;
                }

                if (currentChunkMaxValue >= val)
                    return chunksCount;
                return -1;
            }

            var chunkIndex = GetChunkIndex(value);
            var spanToSearch =
                chunkIndex == chunksCount
                    ? new Span<int>(currentChunk, 0, currentIndexInChunk)
                    : new Span<int>(chunks[chunkIndex].Item2);

            var foundIndex = spanToSearch.BinarySearch(value);
            return (chunkIndex, foundIndex >= 0 ? foundIndex : ~foundIndex);
        }

        private void CreateNextChunk(int key)
        {
            _chunksLock.EnterWriteLock();
            try
            {
                if (_currentChunk != null)
                {
                    _chunks.Add((_currentChunk[^1], _currentChunk));
                }

                _currentChunk = ArrayPool<int>.Shared.Rent(_chunkSize);
                _currentIndexInChunk = 0;
                _chunkSize *= 2;
            }
            finally
            {
                _chunksLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            foreach (var (_, chunk) in _chunks)
            {
                ArrayPool<int>.Shared.Return(chunk);
            }
        }
    }
}