using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogGrokCore.Data.Tests
{
    public class LineIndexMock : ILineIndex
    {
        public int Count => throw new System.NotImplementedException();
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

        public (long offset, int lenghth) GetLine(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}
