using System;

namespace LogGrokCore.Data.Tests
{

    public class LineDataConsumerStub : ILineDataConsumer
    {
        public bool AddLineData(long lineOffset, Span<byte> lineData)
        {
            return lineData[0] == 0;
        }
    }
}
