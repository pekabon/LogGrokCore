using System.Collections.Generic;
using System.Diagnostics;

namespace LogGrokCore.Data
{
    public class LineIndex : ILineIndex
    {
        public (long offset, int length) GetLine(int index)
        {
            Debug.Assert(index < Count);
            lock (_lineStarts)
            {
                var lineStart = _lineStarts[index];
                if (index < _lineStarts.Count - 1)
                    return (lineStart, (int)(_lineStarts[index + 1] - lineStart));
                
#pragma warning disable CS8629 // Nullable value type may be null.
                return (lineStart, _lastLineLength!.Value);
#pragma warning restore CS8629 // Nullable value type may be null.
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

        private readonly List<long> _lineStarts = new List<long>();
        private int? _lastLineLength;
    }
}
