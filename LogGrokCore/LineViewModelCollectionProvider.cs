using System;
using System.Buffers;
using System.Collections.Generic;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    public class LineViewModelCollectionProvider
    {
        private readonly IItemProvider<(int index, string str)> _lineProvider;
        private readonly ILineParser _lineParser;
        private readonly Func<string?> _headerProvider;

        private class FilteredLineProvider: IItemProvider<(int index, string str)>
        {
            private readonly IItemProvider<int> _lineNumbersProvider;
            private readonly IItemProvider<(int index, string str)> _lineProvider;
            
            public FilteredLineProvider(
                IItemProvider<int> lineNumbersProvider,
                IItemProvider<(int index, string str)> lineProvider)
            {
                _lineNumbersProvider = lineNumbersProvider;
                _lineProvider = lineProvider;
            }

            public int Count => _lineNumbersProvider.Count;
            public void Fetch(int start, Span<(int, string)> values)
            {
                using var owner = MemoryPool<int>.Shared.Rent(values.Length);
                var lineNumbers = owner.Memory.Span.Slice(0, values.Length);
                _lineNumbersProvider.Fetch(start, lineNumbers);
                FetchRanges(lineNumbers, values);
            }

            private void FetchRanges(Span<int> lineNumbers, Span<(int, string)> values)
            {
                var sourceRangeStart = -1;
                var sourceRangeEnd = 0;
                var targetRangeStart = 0;
                foreach (var lineNumber in lineNumbers)
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
                        _lineProvider.Fetch(sourceRangeStart,
                            values.Slice(targetRangeStart, rangeLength));
                        targetRangeStart += rangeLength;
                        sourceRangeStart = lineNumber;
                        sourceRangeEnd = sourceRangeStart;
                    }
                }

                if (sourceRangeStart >= 0)
                {
                    _lineProvider.Fetch(sourceRangeStart, values.Slice(targetRangeStart, sourceRangeEnd - sourceRangeStart + 1));
                }
            }
        }

        public LineViewModelCollectionProvider(
            IItemProvider<(int index, string str)> lineProvider,
            ILineParser lineParser,
            Func<string?> headerProvider)
        {
            _lineProvider = lineProvider;
            _lineParser = lineParser;
            _headerProvider = headerProvider;
        }

        public (GrowingLogLinesCollection lineViewModelsCollection, Func<int, int> getIndexByValue) 
            GetLogLinesCollection(
                Indexer indexer,
                IReadOnlyDictionary<int, IEnumerable<string>> exclusions)
        {
            var (itemProvider, getIndexByValue) = GetLineProvider(indexer,exclusions);
            var lineCollection =
                new VirtualList<(int index, string str), ItemViewModel>(itemProvider,
                    indexAndString => 
                        new LineViewModel(indexAndString.index, indexAndString.str, _lineParser));
            return (new GrowingLogLinesCollection(() => _headerProvider(), lineCollection),
                        getIndexByValue);
        }
        
        private (IItemProvider<(int index, string str)> itemProvider, Func<int, int> GetIndexByValue) GetLineProvider(
            Indexer indexer,
            IReadOnlyDictionary<int, IEnumerable<string>> exclusions)
        {
            //if (exclusions.Count == 0) return (_lineProvider, x=> x);
            var lineNumbersProvider = indexer.GetIndexedLinesProvider(exclusions);
            return (new FilteredLineProvider(lineNumbersProvider, _lineProvider),
                value => lineNumbersProvider.GetIndexByValue(value));
        }
    }
}