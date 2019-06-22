using System;

namespace LogGrokCore.Data.Tests
{

    public class LineDataConsumerStub : ILineDataConsumer
    {
        public bool AddLineData(Span<byte> lineData)
        {
            return true;
        }
    }
}
