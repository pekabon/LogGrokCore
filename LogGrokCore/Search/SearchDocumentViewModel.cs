using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
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
        
        private GrowingLogLinesCollection? _lines;
        public Action<int>? SelectedIndexChanged;
        private string _title;
        private  bool _isIndeterminateProgress;

        private readonly object _cancellationTokenSourceLock = new();
        private CancellationTokenSource? _currentSearchCancellationTokenSource;
        private bool _isSearching;
        private double _progress;
        private Regex? _highlightRegex;
        private readonly FilterSettings _filterSettings;
        private readonly LineViewModelCollectionProvider _lineViewModelCollectionProvider;
        private Indexer? _currentSearchIndexer;
        private int _currentItemIndex;
        private readonly LogModelFacade _logModelFacade;

        public SearchDocumentViewModel(
            LogModelFacade logModelFacade,
            FilterSettings filterSettings,
            LineViewModelCollectionProvider lineViewModelCollectionProvider,
            GridViewFactory viewFactory,
            SearchPattern searchPattern)
        {
            
            _viewFactory = viewFactory;

            _logModelFacade = logModelFacade;
            _title = searchPattern.Pattern;
            
            _filterSettings = filterSettings;
            _filterSettings.ExclusionsChanged += UpdateLines;
            _lineViewModelCollectionProvider = lineViewModelCollectionProvider;
            
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

        public ViewBase CustomView => _viewFactory.CreateView();

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

        public int CurrentItemIndex
        {
            get => _currentItemIndex;
            set
            {
                SetAndRaiseIfChanged(ref _currentItemIndex, value);
                var lineIndex = (Lines?[_currentItemIndex] as LineViewModel)?.Index;
                if (lineIndex is {} index)
                    SelectedIndexChanged?.Invoke(index);
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
                _logModelFacade,
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