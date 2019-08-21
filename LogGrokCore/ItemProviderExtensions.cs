using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    public static class ItemProviderExtensions
    {
        public static IEnumerable<T> Enumerate<T>(this IItemProvider<T> itemProvider, int desiredBlockSize)
        {
            var buffer = ArrayPool<T>.Shared.Rent(desiredBlockSize);
            try
            {
                var count = itemProvider.Count;
                var blockSize = buffer.Length;
                var blockCount = count / blockSize;
                var tailSize = count % blockSize;
                for (var idx = 0; idx < blockCount; idx++)
                {
                    itemProvider.Fetch(idx * blockSize, buffer);
                    foreach (var item in buffer)
                        yield return item;
                }
                
                itemProvider.Fetch(blockCount*blockSize, buffer.AsSpan().Slice(tailSize));
                foreach (var item in buffer.Take(tailSize))
                    yield return item;
            }
            finally
            {
                ArrayPool<T>.Shared.Return(buffer);
            }
        }
    }
}