using System;
using System.Buffers;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    internal class ItemProviderMapper<T> : IItemProvider<T>
    {
        private readonly IItemProvider<int> _itemNumbersProvider;
        private readonly IItemProvider<T> _itemProvider;

        public ItemProviderMapper(
            IItemProvider<int> itemNumbersProvider, // collection of indices, int -> int
            IItemProvider<T> itemProvider)          // collection of items, int -> T
        {
            _itemNumbersProvider = itemNumbersProvider;
            _itemProvider = itemProvider;
        }

        public int Count => _itemNumbersProvider.Count;
        public void Fetch(int start, Span<T> values)
        {
            using var owner = MemoryPool<int>.Shared.Rent(values.Length);
            var itemNumbers = owner.Memory.Span.Slice(0, values.Length);
            _itemNumbersProvider.Fetch(start, itemNumbers);
            FetchRanges(itemNumbers, values);
        }

        private void FetchRanges(Span<int> itemNumbers, Span<T> values)
        {
            var sourceRangeStart = -1;
            var sourceRangeEnd = 0;
            var targetRangeStart = 0;
            foreach (var lineNumber in itemNumbers)
            {
                if (sourceRangeStart < 0)
                {
                    sourceRangeStart = lineNumber;
                    sourceRangeEnd = sourceRangeStart;
                }
                else if (lineNumber == sourceRangeEnd + 1)
                {
                    sourceRangeEnd = lineNumber;
                }
                else
                {
                    var rangeLength = sourceRangeEnd - sourceRangeStart + 1;
                    _itemProvider.Fetch(sourceRangeStart,
                        values.Slice(targetRangeStart, rangeLength));
                    targetRangeStart += rangeLength;
                    sourceRangeStart = lineNumber;
                    sourceRangeEnd = sourceRangeStart;
                }
            }

            if (sourceRangeStart >= 0)
            {
                _itemProvider.Fetch(sourceRangeStart, values.Slice(targetRangeStart, sourceRangeEnd - sourceRangeStart + 1));
            }
        }
    }
}