using System;

namespace LogGrokCore.Data.Tests
{
    public class LineDataConsumerMock : ILineDataConsumer
    {
        private readonly  LineIndexMock _lineIndexMock;
        public LineDataConsumerMock(LineIndexMock lineIndexMock)
        {
            _lineIndexMock = lineIndexMock;
        }

        public void AddLineData(long lineOffset, Span<byte> lineData)
        {
            var parsed = lineData[0] == 0;
            if (parsed)
            {
                _lineIndexMock.Add(lineOffset);
            }
        }

        public void CompleteAdding(long totalBytesRead)
        {
        }
   }
}
