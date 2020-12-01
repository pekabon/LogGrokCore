using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LogGrokCore.Data.IndexTree;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data
{
    public class LineIndex : ILineIndex, IItemProvider<(long offset, int length)>
    {
        public (long offset, int length) GetLine(int index)
        {
            Debug.Assert(index < Count);
            lock (_lineStarts)
            {
                var lineStart = _lineStarts[index];
                if (index < _lineStarts.Count - 1)
                    return (lineStart, (int)(_lineStarts[index + 1] - lineStart));
                
                if (!_lastLineLength.HasValue)
                    throw new IndexOutOfRangeException();
                return (lineStart, _lastLineLength!.Value);
            }
        }

        public void Fetch(int start, Span<(long offset, int length)> values)
        {
            lock (_lineStarts)
            {
                var lineStartsEnumerable = _lineStarts.GetEnumerableFromIndex(start);
                using var enumerator = lineStartsEnumerable.GetEnumerator();
                if (!enumerator.MoveNext())
                    throw new IndexOutOfRangeException();
                var first = enumerator.Current;

                var index = 0;
                while (enumerator.MoveNext() && index < values.Length)
                {
                    var second = enumerator.Current;
                    values[index] = (first, (int) (second - first));
                    first = second;
                    index++;
                }

                if (index >= values.Length) return;
                
                if (start + index == Count - 1 &&_lastLineLength.HasValue)
                    values[index] = (first, _lastLineLength!.Value);
                else
                    throw new IndexOutOfRangeException();
            }
        }


        public async IAsyncEnumerable<(int start, int count)> FetchRanges(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const int minRangeSize = 256;
            var currentIndex = 0;
            var currentCount = Count;

            while (currentIndex < currentCount || !IsFinished)
            {
                try
                {
                    while (currentIndex + minRangeSize > currentCount && !IsFinished)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                        currentCount = Count;
                    }
                }
                catch (TaskCanceledException)
                {
                    yield break;
                }

                if (cancellationToken.IsCancellationRequested)
                    yield break;

                
                var rangeSize = currentCount - currentIndex;
                yield return (currentIndex, rangeSize);
                currentIndex += rangeSize;
                currentCount = Count;
            }
        }

        public int Count
        {
            get
            {
                lock (_lineStarts)
                    return (_lastLineLength, _lineStarts.Count) switch
                        {
                            (_, 0) => 0,
                            (null, var count) => count - 1,    
                            var (_, count) => count
                        };
            }
        }

        public int Add(long lineStart)
        {
            lock (_lineStarts)
            {
                var lineNum = _lineStarts.Count;
                _lineStarts.Add(lineStart);
                return lineNum;
            }
        }

        public void Finish(int lastLength)
        {
            _lastLineLength = lastLength;
        }
        
        public bool IsFinished => _lastLineLength.HasValue;

        private readonly IndexTree<long, LongsLeaf> _lineStarts 
            = new(16, l => new LongsLeaf(l, 0));
        private int? _lastLineLength;
    }
}
