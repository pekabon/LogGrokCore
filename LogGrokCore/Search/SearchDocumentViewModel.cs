using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Data;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Search
{
    internal class SearchDocumentViewModel : ViewModelBase
    {
        private readonly GridViewFactory _viewFactory;
        private SearchPattern _searchPattern;
        private readonly LogFile _logFile;
        private readonly LineIndex _lineIndex;
        private readonly ILineParser _lineParser;
        private GrowingLogLinesCollection? _lines;


        public SearchDocumentViewModel(
            LogFile logFile,
            LineIndex lineIndex,
            ILineParser lineParser,
            GridViewFactory viewFactory, 
            SearchPattern searchPattern)
        {
            _viewFactory = viewFactory;
            _searchPattern = searchPattern;

            _logFile = logFile;
            _lineIndex = lineIndex;
            _lineParser = lineParser;    
            Title = searchPattern.Pattern;
            AddToScratchPadCommand = new DelegateCommand(() => throw new NotImplementedException());
        }

        public string Title { get; }

        public bool IsIndeterminateProgress { get; private set; }

        public bool IsSearching { get; private set; }

        public double SearchProgress { get; private set; }

        public GrowingLogLinesCollection? Lines
        {
            get => _lines;
            private set => SetAndRaiseIfChanged(ref _lines,  value);
        }

        public object? SelectedValue { get; set; }
        
        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand AddToScratchPadCommand { get; private set; }

        public void SetSearchPattern(SearchPattern searchPattern)
        {
            _searchPattern = searchPattern;
            StartSearch();
        }

        private void StartSearch()
        {
            var searchLineIndex = Data.Search.Search.CreateSearchIndex(
                _logFile.OpenForSequentialRead(),
                _logFile.Encoding,
                _lineIndex,
                _searchPattern.GetRegex(RegexOptions.Compiled));
            var lineProvider =  new LineProvider(searchLineIndex, _logFile);
            var lineCollection =
                new VirtualList<string, ItemViewModel>(lineProvider,
                    (str, index) => new LineViewModel(index, str, _lineParser));

            Lines = new GrowingLogLinesCollection(() => null,
                lineCollection);
        }
    }
}