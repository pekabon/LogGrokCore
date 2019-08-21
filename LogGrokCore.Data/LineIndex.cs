using System;
using System.Diagnostics;
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
            = new IndexTree<long, LongsLeaf>(16, l => new LongsLeaf(l, 0));
        private int? _lastLineLength;
    }
}
