using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Data;
using LogGrokCore.Data.Search;
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

            _logFile = logFile;
            _lineIndex = lineIndex;
            _lineParser = lineParser;    
            Title = searchPattern.Pattern;
            AddToScratchPadCommand = new DelegateCommand(() => throw new NotImplementedException());
            SetSearchPattern(searchPattern);
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

        public object? SelectedValue
        {
            get => _selectedValue;
            set
            {
                if (_selectedValue == value || value == null) return;
                _selectedValue = value;
                if (_selectedValue is LineViewModel lineViewModel)
                {
                    SelectedIndexChanged?.Invoke(lineViewModel.Index);
                }
            }
        }

        public Action<int>? SelectedIndexChanged;
        private object? _selectedValue;

        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand AddToScratchPadCommand { get; private set; }

        public void SetSearchPattern(SearchPattern searchPattern)
        {
            _searchPattern = searchPattern;
            StartSearch();
        }

        private void StartSearch()
        {
            var (progress, searchLineIndex) = Data.Search.Search.CreateSearchIndex(
                _logFile.OpenForSequentialRead(),
                _logFile.Encoding,
                _lineIndex,
                _searchPattern.GetRegex(RegexOptions.Compiled));
            
            var lineProvider =  new SearchLineProvider(searchLineIndex, _logFile);
            var lineCollection =
                new VirtualList<(int, string), ItemViewModel>(lineProvider,
                    (value, _) =>
                    {
                        var (originalIndex, str) = value;
                        return new LineViewModel(originalIndex, str, _lineParser);
                    });

            Lines = new GrowingLogLinesCollection(() => null,
                lineCollection);


            var context = SynchronizationContext.Current;

            void UpdateCount()
            {
                context?.Post(n => Lines?.UpdateCount(), null);
            }

            progress.Changed += _ => UpdateCount();
            progress.IsFinishedChanged += UpdateCount;

        }
    }
}