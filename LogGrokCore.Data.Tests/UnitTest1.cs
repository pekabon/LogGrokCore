using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogGrokCore.Data.Tests
{
    internal class LineIndex : ILineIndex
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

    [TestClass]
    public class LoaderImpTests
    {
        private byte[] _cr = Encoding.UTF8.GetBytes("\r");
        private byte[] _lf = Encoding.UTF8.GetBytes("\n");

        private int CrlfLength => _cr.Length + _lf.Length;
        private const int BufferSize = 256;
        
        [TestMethod]
        public void CheckNoCrlfsInBuffer()
        {
            var buffer = new byte[BufferSize];
            var lineIndex = DoLoad(buffer);
            Assert.AreEqual(1, lineIndex.LineStarts.Count, "Count");
            Assert.AreEqual(BufferSize, lineIndex.LastLength, "LastLength");
        }

        [TestMethod]
        public void CheckNoCrlfsInBuffer25()
        {
            var bufferSize = (int)(BufferSize * 2.5);
            var buffer = new byte[bufferSize];
            var lineIndex = DoLoad(buffer);
            Assert.AreEqual(1, lineIndex.LineStarts.Count, "Count");
            Assert.AreEqual(bufferSize, lineIndex.LastLength, "LastLength");
        }

        [TestMethod]
        public void CheckCrllfsAtTheEnd()
        {
            var buffer = new byte[BufferSize];
            AddCrlf(buffer, buffer.Length - CrlfLength);
            var lineIndex = DoLoad(buffer);
            Assert.AreEqual(1, lineIndex.LineStarts.Count, "Count");
            Assert.AreEqual(BufferSize, lineIndex.LastLength, "LastLength");
        }

        [TestMethod]
        public void CheckLineAtTheBorder()
        {
            var buffer = new Byte[BufferSize * 2];
            AddCrlf(buffer, BufferSize / 2);
            AddCrlf(buffer, BufferSize / 2 + BufferSize);
            var lineIndex = DoLoad(buffer);

            var lineStarts = lineIndex.LineStarts;
            Assert.AreEqual(3, lineStarts.Count);
            Assert.AreEqual(0, lineStarts[0]);
            Assert.AreEqual(BufferSize / 2 + CrlfLength, lineStarts[1]);
            Assert.AreEqual(BufferSize / 2 + BufferSize + CrlfLength, lineStarts[2]);
            Assert.AreEqual(BufferSize * 2 - lineStarts[2], lineIndex.LastLength);
        }

        private void AddCrlf(byte[] buffer, int position)
        {
            _cr.CopyTo(buffer, position);
            _lf.CopyTo(buffer, position + _cr.Length);
        }

        private LineIndex DoLoad(byte[] buffer)
        {
            var lineIndex = new LineIndex();
            var loader = new LoaderImpl(BufferSize, lineIndex);
            var stream = new MemoryStream(buffer);
            loader.Load(stream, _cr.AsSpan(), _lf.AsSpan());
            return lineIndex;
        }
    }
}
