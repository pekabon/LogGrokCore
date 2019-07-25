using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using ImTools;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    public class LineViewModelCollectionProvider
    {
        private readonly Indexer _indexer;
        private readonly Dictionary<int, List<string>> _exclusions = new Dictionary<int, List<string>>();
        private readonly IItemProvider<string> _lineProvider;
        private readonly ILineParser _lineParser;
        private readonly HeaderProvider _headerProvider;
        private readonly LogMetaInformation _metaInformation;
        
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
            Indexer indexer,
            LineIndex lineIndex,
            IItemProvider<string> lineProvider,
            ILineParser lineParser,
            HeaderProvider headerProvider,
            LogMetaInformation metaInformation)
        {
            _indexer = indexer;
            _lineProvider = lineProvider;
            _lineParser = lineParser;
            _headerProvider = headerProvider;
            _metaInformation = metaInformation;
        }

        public GrowingLogLinesCollection GetLogLinesCollection()
        {
            var lineProvider = GetLineProvider();
            var lineCollection =
                new VirtualList<string, ItemViewModel>(lineProvider,
                    (str, index) => new LineViewModel(index, str, _lineParser));

            return new GrowingLogLinesCollection(_headerProvider, lineCollection);
        }

        public bool HaveExclusions => _exclusions.Count > 0;

        public event Action ExclusionsChanged;

        public void AddExclusions(int component, IEnumerable<string> componentValuesToExclude)
        {
            var indexedComponent = GetIndexedComponent(component);

            List<string>? currentExclusions;
            if (!_exclusions.TryGetValue(GetIndexedComponent(indexedComponent), out currentExclusions))
            {
                currentExclusions = new List<string>();
            }

            SetExclusions(indexedComponent, currentExclusions.Concat(componentValuesToExclude));
        }

        public void ExcludeAllExcept(int component, IEnumerable<string> componentValuesToInclude)
        {
            var indexedComponent = GetIndexedComponent(component);

            var exclusions = _indexer.GetAllComponents(indexedComponent).Except(componentValuesToInclude);
            SetExclusions(indexedComponent , exclusions);
        }

        public void ClearAllExclusions()
        {
            _exclusions.Clear();
            ExclusionsChanged?.Invoke();
        }

        private void SetExclusions(int indexedComponent, IEnumerable<string> componentValuesToExclude)
        {
            _exclusions[indexedComponent] = componentValuesToExclude.ToList();
            ExclusionsChanged?.Invoke();
        }
        
        private int GetIndexedComponent(int component) => _metaInformation.IndexedFieldNumbers.IndexOf(component);
        
        private IItemProvider<string> GetLineProvider()
        {
            if (_exclusions.Count == 0) return _lineProvider;
            var lineNumbersProvider = _indexer.GetIndexedLinesProvider(_exclusions);
            return new FilteredLineProvider(lineNumbersProvider, _lineProvider);
        }

    }
}