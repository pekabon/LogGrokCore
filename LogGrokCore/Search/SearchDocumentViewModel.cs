using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;
using LogGrokCore.Filter;

namespace LogGrokCore.Search
{
    internal class SearchDocumentViewModel : ViewModelBase, IDisposable
    {
        private readonly GridViewFactory _viewFactory;
        private SearchPattern _searchPattern;
        private readonly LogFile _logFile;
        private readonly LineIndex _lineIndex;
        
        private GrowingLogLinesCollection? _lines;
        public Action<int>? SelectedIndexChanged;
        private object? _selectedValue;
        private string _title;
        private  bool _isIndeterminateProgress;

        private readonly object _cancellationTokenSourceLock = new();
        private CancellationTokenSource? _currentSearchCancellationTokenSource;
        private bool _isSearching;
        private double _progress;
        private Regex? _highlightRegex;
        private readonly Indexer _indexer;
        private readonly FilterSettings _filterSettings;
        private readonly LineViewModelCollectionProvider _lineViewModelCollectionProvider;
        private Indexer? _currentSearchIndexer;

        public SearchDocumentViewModel(
            LogFile logFile,
            LineIndex lineIndex,
            Indexer indexer,
            FilterSettings filterSettings,
            LineViewModelCollectionProvider lineViewModelCollectionProvider,
            GridViewFactory viewFactory, 
            SearchPattern searchPattern)
        {
            _viewFactory = viewFactory;

            _logFile = logFile;
            _lineIndex = lineIndex;
            _title = searchPattern.Pattern;
            _indexer = indexer;
            
            _filterSettings = filterSettings;
            _filterSettings.ExclusionsChanged += UpdateLines;
            _lineViewModelCollectionProvider = lineViewModelCollectionProvider;
            AddToScratchPadCommand = new DelegateCommand(() => throw new NotImplementedException());
            SearchPattern = searchPattern;
        }

        public string Title
        {
            get => _title;
            set => SetAndRaiseIfChanged(ref _title, value);
        }

        public bool IsIndeterminateProgress
        {
            get => _isIndeterminateProgress;
            set => SetAndRaiseIfChanged(ref _isIndeterminateProgress, value); 
        }

        public bool IsSearching         
        {
            get => _isSearching;
            set => SetAndRaiseIfChanged(ref _isSearching, value); 
        }

        public double SearchProgress 
        {
            get => _progress;
            set => SetAndRaiseIfChanged(ref _progress, value); 
        }

        public Regex? HighlightRegex
        {
            get => _highlightRegex;
            set => SetAndRaiseIfChanged(ref _highlightRegex, value);
        }

        
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

                var lineViewModel =
                    _selectedValue as LineViewModel ??
                    (_selectedValue as FrameworkElement)?.DataContext as LineViewModel;

                if (lineViewModel != null)
                {
                    SelectedIndexChanged?.Invoke(lineViewModel.Index);
                }
            }
        }

        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand AddToScratchPadCommand { get; private set; }

        public SearchPattern SearchPattern
        {
            get => _searchPattern;
            set
            {
                if (_searchPattern.Equals(value)) return;
                _searchPattern = value;
                Title = _searchPattern.Pattern;
                HighlightRegex = _searchPattern.GetRegex(RegexOptions.None);
                StartSearch();
            }
        }

        private void StartSearch()
        {
            lock (_cancellationTokenSourceLock)
            {
                _currentSearchCancellationTokenSource?.Cancel();
                _currentSearchCancellationTokenSource = new CancellationTokenSource();
            }

            SearchProgress = 0;
            IsIndeterminateProgress = true;
            IsSearching = true;
            
            var (progress, searchIndexer) = Data.Search.Search.CreateSearchIndex(
                _logFile.OpenForSequentialRead(),
                _logFile.Encoding,
                _indexer,
                _lineIndex,
                _searchPattern.GetRegex(RegexOptions.Compiled),
                _currentSearchCancellationTokenSource.Token);

            _currentSearchIndexer = searchIndexer;
            
            UpdateLines();

            UpdateDocumentWhileLoading(progress);
        }
        
        private async void UpdateDocumentWhileLoading(Data.Search.Search.Progress progress)
        {
            var delay = 10;
            IsSearching = true;
            while (!progress.IsFinished)
            {
                await Task.Delay(delay);
                if (delay < 150)
                    delay *= 2;
                Lines?.UpdateCount();
                IsIndeterminateProgress = false;
                SearchProgress = progress.Value * 100.0;
            }

            Lines?.UpdateCount();
            SearchProgress = progress.Value * 100.0;
            IsSearching = false;
        }

        private void UpdateLines()
        {
            if (_currentSearchIndexer == null) return;
            var (lineViewModelsCollection, _)
                = _lineViewModelCollectionProvider.GetLogLinesCollection(_currentSearchIndexer, _filterSettings.Exclusions);

            Lines = lineViewModelsCollection;
        }

        public void Dispose()
        {
            lock (_cancellationTokenSourceLock)
            {
                _currentSearchCancellationTokenSource?.Cancel();
                _currentSearchCancellationTokenSource = null;
            }
        }
    }
}