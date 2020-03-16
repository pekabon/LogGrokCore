using System.Collections.Generic;

namespace LogGrokCore.Data
{
    public interface ILineIndex
    {
        int Count { get; }
        
        (long offset, int length) GetLine(int index);
    }
}