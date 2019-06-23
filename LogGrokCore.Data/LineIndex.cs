using System.Collections.Generic;
using System.Diagnostics;

namespace LogGrokCore.Data
{
    public class LineIndex : ILineIndex
    {
        public (long offset, int lenghth) GetLine(int index)
        {
            Debug.Assert(index < Count);
            lock (_lineStarts)
            {
                var lineStart = _lineStarts[index];
                if (index < Count - 1)
                    return (lineStart, (int)(_lineStarts[index + 1] - lineStart));
                else
#pragma warning disable CS8629 // Nullable value type may be null.
                    return (lineStart, _lastLineLength.Value);
#pragma warning restore CS8629 // Nullable value type may be null.
            }
        }

        public int Count
        {
            get
            {
                lock (_lineStarts)
                    //return _lastLineLength.HasValue? _lineStarts.Count: _lineStarts.Count - 1;
                    return _lineStarts.Count;

            }
        }

        public void Add(long lineStart)
        {
            lock (_lineStarts)
                _lineStarts.Add(lineStart);
        }

        public void Finish(int lastLength)
        {
            _lastLineLength = lastLength;
        }

        private readonly List<long> _lineStarts = new List<long>();
        private int? _lastLineLength;
    }
}
