using System;

namespace LogGrokCore.Data
{
    public interface ILineDataConsumer
    {
        void AddLineData(long offset, Span<byte> lineData);

        void CompleteAdding(long totalBytesRead);
    }
}