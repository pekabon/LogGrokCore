using System;
using System.Collections.Generic;

namespace LogGrokCore.Data.Tests
{
    public class LineIndexMock : ILineIndex
    {
        public int Count => throw new NotImplementedException();
        public List<long> LineStarts { get; } = new List<long>();
        public int LastLength { get; private set; }

        public int Add(long lineStart)
        {
            var lineNum = LineStarts.Count;
            LineStarts.Add(lineStart);
            return lineNum;
        }

        public void Finish(int lastLength)
        {
            LastLength = lastLength;
        }

        public (long offset, int length) GetLine(int index)
        {
            throw new NotImplementedException();
        }

        public void Fetch(int start, Span<(long offset, int length)> values)
        {
            throw new NotImplementedException();
        }
    }
}
