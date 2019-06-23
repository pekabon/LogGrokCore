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
        public void CheckNoCrlfsAndNoLinesInBuffer()
        {
            CheckLines(BufferSize, true);
        }
        
        [TestMethod]
        public void CheckNoCrlfsInBuffer25()
        {
            CheckLines((int)(BufferSize * 2.5));
        }
        
        [TestMethod]
        public void CheckNoCrlfsAndNoLinesInBuffer25()
        {
            CheckLines((int)(BufferSize * 2.5), true);
        }

        [TestMethod]
        public void CheckCrllfsAtTheEnd()
        {
            CheckLines(BufferSize, BufferSize - CrlfLength);
        }
        
        [TestMethod]
        public void CheckCrllfsAtTheEndWithHeader()
        {
            CheckLines(BufferSize, true, BufferSize - CrlfLength);
        }

        [TestMethod]
        public void CheckLineBetweenBuffers()
        {
            CheckLines(BufferSize * 2,
                    BufferSize / 2,
                    BufferSize / 2 + BufferSize);
        }

        [TestMethod]
        public void CheckLineBetweenBuffersWithHeader()
        {
            CheckLines(BufferSize * 2, true,
                BufferSize / 2,
                BufferSize / 2 + BufferSize);
        }

        [TestMethod]
        public void CheckLongLine()
        {
            CheckLines(BufferSize * 10);
        }
        
        [TestMethod]
        public void CheckLongHeader()
        {
            CheckLines(BufferSize * 10, true);
        }

        [TestMethod]
        public void CheckCrLfOnBorder()
        {
            CheckLines(BufferSize * 2, BufferSize - CrlfLength / 2);
        }
        
        [TestMethod]
        public void CheckCrLfOnBorderWithHeader()
        {
            CheckLines(BufferSize * 2, true, BufferSize - CrlfLength / 2);
        }

        [TestMethod]
        public void CheckShortLinesAfterLong()
        {
            var (crlfPositions, bufferSize) = CreateShortLinesAfterLong();

            CheckLines(bufferSize, crlfPositions);
        }
        
        [TestMethod]
        public void CheckShortLinesAfterLongWithHeader()
        {
            var (crlfPositions, bufferSize) = CreateShortLinesAfterLong();

            CheckLines(bufferSize, true, crlfPositions);
        }

        private static (int[] crlfPositions, int bufferSize) CreateShortLinesAfterLong()
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
            return (crlfPositions, bufferSize);
        }

        private void CheckLines(int bufferSize, params int[] crlfPositions)
        {
            CheckLines(bufferSize, false, crlfPositions);
        }

        private void CheckLines(int bufferSize, bool haveHeader, params int[] crlfPositions)
        {
            Array.Sort(crlfPositions);
            var buffer = new byte[bufferSize];
            foreach (var crlfPosition in crlfPositions)
            {
                AddCrlf(buffer, crlfPosition);
            }

            if (haveHeader)
            {
                buffer[0] = 1;
            }
            
            var lineIndex = DoLoad(buffer);
            var lineStarts = lineIndex.LineStarts;

            bool haveLastLine = !crlfPositions.Any(position => position == bufferSize - CrlfLength);
            var supposedLineStartsCount = crlfPositions.Length + (haveLastLine ? 1 : 0) - (haveHeader ? 1 : 0);

            Assert.AreEqual(supposedLineStartsCount, lineStarts.Count);

            if (supposedLineStartsCount == 0)
                return;
            
            var supposedLastLineLength = bufferSize - lineStarts.Max();
            Assert.AreEqual(supposedLastLineLength, lineIndex.LastLength);

            if (!haveHeader)
            {
                Assert.AreEqual(0, lineStarts[0]);
                for (var idx = 0; idx < crlfPositions.Length - (haveLastLine ? 0 : 1); idx++)
                {
                    Assert.AreEqual(crlfPositions[idx] + CrlfLength, lineStarts[idx + 1]);
                }
            }
            else
            {
                Assert.AreEqual(crlfPositions[0] + CrlfLength, lineStarts[0]);
                for (var idx = 1; idx < crlfPositions.Length - (haveLastLine ? 0 : 1); idx++)
                {
                    Assert.AreEqual(crlfPositions[idx] + CrlfLength, lineStarts[idx]);
                }
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
            var dataConsumer = new LineDataConsumerStub();
            var loader = new LoaderImpl(BufferSize, lineIndex, dataConsumer);
            var stream = new MemoryStream(buffer);
            loader.Load(stream, _cr.AsSpan(), _lf.AsSpan());
            return lineIndex;
        }
    }
}
