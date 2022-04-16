using System;
using System.Buffers;
using System.Threading.Tasks;

namespace LogGrokCore.Data.Tests
{
    public class LineDataConsumerMock : ILineDataConsumer
    {
        private readonly  LineIndexMock _lineIndexMock;
        public LineDataConsumerMock(LineIndexMock lineIndexMock)
        {
            _lineIndexMock = lineIndexMock;
        }

        public Task AddLineData(IMemoryOwner<byte> memory, PooledList<(long offset, int start, int length)> lines)
        {
            
            var mem = memory.Memory;
            foreach (var (offset, start, _) in lines)
            {
                if (mem.Span[start..][0] == 0)
                {
                    _lineIndexMock.Add(offset);
                }
            }
            memory.Dispose();
            var taskCompletionSource = new TaskCompletionSource();
            var task = taskCompletionSource.Task;
            taskCompletionSource.SetResult();
            return task;
        }
        
        public Task CompleteAdding(long totalBytesRead) => Task.CompletedTask;
    }
}
