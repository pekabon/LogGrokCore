using System;

namespace LogGrokCore.Data.Tests
{

    public class LineDataConsumerStub : ILineDataConsumer
    {
        public bool AddLineData(Span<byte> lineData)
        {
            return lineData[0] == 0;
        }
    }
}
