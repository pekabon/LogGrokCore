using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogGrokCore.Data.Tests
{
    internal class LineIndexMock : ILineIndex
    {
        public int Count => throw new System.NotImplementedException();
        public List<long> LineStarts { get; } = new List<long>();
        public int LastLength { get; private set; }

        public void Add(long lineStart)
        {
            LineStarts.Add(lineStart);
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
