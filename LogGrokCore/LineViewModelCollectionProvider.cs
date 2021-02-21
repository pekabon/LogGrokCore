using System;
using System.Collections.Generic;
using LogGrokCore.Controls;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    public class LineViewModelCollectionProvider
    {
        private readonly IItemProvider<(int index, string str)> _lineProvider;
        private readonly ILineParser _lineParser;
        private readonly IReadOnlyList<ItemViewModel> _headerCollection;
        private readonly Selection _markedLines;

        public LineViewModelCollectionProvider(
            IItemProvider<(int index, string str)> lineProvider,
            ILineParser lineParser,             
            IReadOnlyList<ItemViewModel> headerCollection,
            Selection markedLines)       
        {
            _lineProvider = lineProvider;
            _lineParser = lineParser;
            _headerCollection = headerCollection;

            _markedLines = markedLines;
        }

        public (GrowingLogLinesCollection lineViewModelsCollection, Func<int, int> getIndexByValue) 
            GetLogLinesCollection(
                Indexer indexer, // index: Components -> log line numbers
                IReadOnlyDictionary<int, IEnumerable<string>> exclusions)
        {
            var (itemProvider, getIndexByValue) = GetLineProvider(indexer,exclusions);
            var lineCollection =
                new VirtualList<(int index, string str), ItemViewModel>(itemProvider,
                    indexAndString => 
                        new LineViewModel(indexAndString.index, indexAndString.str, _lineParser, _markedLines));
            return (new GrowingLogLinesCollection(
                    _headerCollection,
                    lineCollection),
                    getIndexByValue);
        }
        
        private (IItemProvider<(int index, string str)> itemProvider, Func<int, int> GetIndexByValue) GetLineProvider(
            Indexer indexer,
            IReadOnlyDictionary<int, IEnumerable<string>> exclusions)
        {
            //if (exclusions.Count == 0) return (_lineProvider, x=> x);
            var lineNumbersProvider = indexer.GetIndexedLinesProvider(exclusions);
            return (new ItemProviderMapper<(int index, string str)>(lineNumbersProvider, _lineProvider),
                value => lineNumbersProvider.GetIndexByValue(value));
        }
    }
}