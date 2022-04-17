using System.Buffers;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{
    public interface ILineDataConsumer
    {
        Task CompleteAdding(long totalBytesRead);

        Task AddLineData(
            IMemoryOwner<byte> memory,
            PooledList<(long offset, int start, int length)> lines);
    }
}