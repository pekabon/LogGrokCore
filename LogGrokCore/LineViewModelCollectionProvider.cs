using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    public class LineViewModelCollectionProvider
    {
        private readonly Indexer _indexer;
        private readonly Dictionary<int, List<string>> _exclusions = new Dictionary<int, List<string>>();
        private readonly LineIndex _lineIndex;
        private readonly IItemProvider<string> _lineProvider;
        private readonly ILineParser _lineParser;
        private readonly HeaderProvider _headerProvider;

        private class FilteredLineProvider: IItemProvider<string>
        {
            private readonly IItemProvider<int> _lineNumbersProvider;
            private readonly IItemProvider<string> _lineProvider;
            
            public FilteredLineProvider(
                IItemProvider<int> lineNumbersProvider,
                IItemProvider<string> lineProvider)
            {
                _lineNumbersProvider = lineNumbersProvider;
                _lineProvider = lineProvider;
            }

            public int Count => _lineNumbersProvider.Count;
            public void Fetch(int start, Span<string> values)
            {
                using var owner = MemoryPool<int>.Shared.Rent(values.Length);
                var lineNumbers = owner.Memory.Span.Slice(0, values.Length);
                _lineNumbersProvider.Fetch(start, lineNumbers);
                FetchRanges(lineNumbers, values);
            }

            private void FetchRanges(Span<int> lineNumbers, Span<string> values)
            {
                var currentRangeStart = -1;
                var currentRangeEnd = 0;
                
                foreach (var lineNumber in lineNumbers)
                {
                    if (currentRangeStart < 0)
                    {
                        currentRangeStart = lineNumber;
                        currentRangeEnd = currentRangeStart;
                    }
                    else if (lineNumber == currentRangeEnd + 1)
                    {
                        currentRangeEnd = lineNumber;
                    }
                    else
                    {
                        _lineProvider.Fetch(currentRangeStart,
                            values.Slice(currentRangeStart, currentRangeEnd - currentRangeStart + 1));
                        currentRangeStart = lineNumber;
                        currentRangeEnd = currentRangeStart;
                    }
                }

                if (currentRangeStart >= 0)
                {
                    _lineProvider.Fetch(currentRangeStart, values.Slice(currentRangeStart, currentRangeEnd - currentRangeStart + 1));
                }
            }
        }

        public LineViewModelCollectionProvider(
            Indexer indexer,
            LineIndex lineIndex,
            IItemProvider<string> lineProvider,
            ILineParser lineParser,
            HeaderProvider headerProvider)
        {
            _indexer = indexer;
            _lineIndex = lineIndex;
            _lineProvider = lineProvider;
            _lineParser = lineParser;
            _headerProvider = headerProvider;
        }

        public GrowingLogLinesCollection GetLogLinesCollection()
        {

            var lineProvider = GetLineProvider();
            var lineCollection =
                new VirtualList<string, ItemViewModel>(lineProvider,
                    (str, index) => new LineViewModel(index, str, _lineParser));

            return new GrowingLogLinesCollection(_headerProvider, lineCollection);
        }

        public IItemProvider<int> GetLineNumbersProvider()
        {
            
            return _indexer.GetIndexedLinesProvider(_exclusions);
        }

        public void SetExlusions(int component, IEnumerable<string> componentValuesToExclude)
        {
            _exclusions[component] = componentValuesToExclude.ToList();
        }
        
        private IItemProvider<string> GetLineProvider()
        {
            if (_exclusions.Count == 0) return _lineProvider;
            var lineNumbersProvider = _indexer.GetIndexedLinesProvider(_exclusions);
            return new FilteredLineProvider(lineNumbersProvider, _lineProvider);
        }

    }
}