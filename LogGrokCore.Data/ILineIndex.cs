using System;

namespace LogGrokCore.Data
{
    public interface ILineIndex
    {
        int Count { get; }
        
        (long offset, int length) GetLine(int index);

        void Fetch(int start, Span<(long offset, int length)> values);
    }
}