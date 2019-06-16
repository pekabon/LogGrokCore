using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LogGrokCore.Data.Tests
{

    [TestClass]
    public class LoaderImpTests
    {
        private readonly byte[] _cr = Encoding.UTF8.GetBytes("\r");
        private readonly byte[] _lf = Encoding.UTF8.GetBytes("\n");

        private int CrlfLength => _cr.Length + _lf.Length;
        private const int BufferSize = 256;

        [TestMethod]
        public void CheckNoCrlfsInBuffer()
        {
            CheckLines(BufferSize);
        }

        [TestMethod]
        public void CheckNoCrlfsInBuffer25()
        {
            CheckLines((int)(BufferSize * 2.5));
        }

        [TestMethod]
        public void CheckCrllfsAtTheEnd()
        {
            CheckLines(BufferSize, BufferSize - CrlfLength);
        }

        [TestMethod]
        public void CheckLineBetweenBuffers()
        {
            CheckLines(BufferSize * 2,
                    BufferSize / 2,
                    BufferSize / 2 + BufferSize);
        }


        [TestMethod]
        public void CheckLongLine()
        {
            CheckLines(BufferSize * 10);
        }

        [TestMethod]
        public void CheckCrLfOnBorder()
        {
            CheckLines(BufferSize * 2, BufferSize - CrlfLength / 2);
        }

        [TestMethod]
        public void CheckShortLinesAfterLong()
        {
            var position = 0;
            var crlfPositions = new[]
            {
                position = BufferSize + BufferSize / 2,
                position += BufferSize / 3,
                position += BufferSize / 3,
                position += BufferSize / 4,
                position += BufferSize / 4,
                position += BufferSize / 4,
                position += BufferSize / 4,
                position += BufferSize / 4,
                position += BufferSize / 4,
                position += BufferSize / 4,
                position += BufferSize / 4,
                position += BufferSize / 4,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
                position += BufferSize / 5,
            };

            var bufferSize = (crlfPositions.Last() / BufferSize + 1) * BufferSize;

            CheckLines(bufferSize, crlfPositions);
        }

        private void CheckLines(int bufferSize, params int[] crlfPositions)
        {
            Array.Sort(crlfPositions);
            var buffer = new byte[bufferSize];
            foreach (var crlfPosition in crlfPositions)
            {
                AddCrlf(buffer, crlfPosition);
            }
            var lineIndex = DoLoad(buffer);
            var lineStarts = lineIndex.LineStarts;

            bool haveLastLine = !crlfPositions.Any(position => position == bufferSize - CrlfLength);
            var supposedLineStartsCount = crlfPositions.Length + (haveLastLine ? 1 : 0);

            Assert.AreEqual(supposedLineStartsCount, lineStarts.Count);

            var supposedLastLineLength = bufferSize - lineStarts.Max();
            Assert.AreEqual(supposedLastLineLength, lineIndex.LastLength);

            Assert.AreEqual(0, lineStarts[0]);
            for (var idx = 0; idx < crlfPositions.Length - (haveLastLine ? 0 : 1); idx++)
            {
                Assert.AreEqual(crlfPositions[idx] + CrlfLength, lineStarts[idx + 1]);
            }
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
