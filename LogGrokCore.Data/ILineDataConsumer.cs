using System;

namespace LogGrokCore.Data
{
    public interface ILineDataConsumer
    {
        bool AddLineData(long offset, Span<byte> lineData);

        void CompleteAdding(long totalBytesRead);
    }
}