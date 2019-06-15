using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogGrokCore.Data.Tests
{

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
        public void CheckLineBetweenBuffers()
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

        [TestMethod]
        public void CheckLongLine()
        {
            var bufferSize = BufferSize * 10;
            var buffer = new byte[bufferSize];

            var lineIndex = DoLoad(buffer);
            var lineStarts = lineIndex.LineStarts;

            Assert.AreEqual(1, lineStarts.Count);
            Assert.AreEqual(bufferSize, lineIndex.LastLength);
        }

        [TestMethod]
        public void CheckCrLfOnBorder()
        {
            var bufferSize = BufferSize * 2;
            var buffer = new byte[bufferSize];

            AddCrlf(buffer, BufferSize - CrlfLength / 2);

            var lineIndex = DoLoad(buffer);
            var lineStarts = lineIndex.LineStarts;

            Assert.AreEqual(2, lineStarts.Count);
            Assert.AreEqual(0, lineStarts[0]);
            Assert.AreEqual(BufferSize + 1, lineStarts[1]);
            Assert.AreEqual(BufferSize - 1, lineIndex.LastLength);
        }

        private void AddCrlf(byte[] buffer, int position)
        {
            _cr.CopyTo(buffer, position);
            _lf.CopyTo(buffer, position + _cr.Length);
        }

        private LineIndexMock DoLoad(byte[] buffer)
        {
            var lineIndex = new LineIndexMock();
            var loader = new LoaderImpl(BufferSize, lineIndex);
            var stream = new MemoryStream(buffer);
            loader.Load(stream, _cr.AsSpan(), _lf.AsSpan());
            return lineIndex;
        }
    }
}
