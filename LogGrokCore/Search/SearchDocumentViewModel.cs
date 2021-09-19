using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Controls;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Controls.ListControls.VirtualizingStackPanel;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Search;
using LogGrokCore.Data.Virtualization;
using LogGrokCore.Filter;

namespace LogGrokCore.Search
{
    public class SearchDocumentViewModel : ViewModelBase, IDisposable
    {
        private readonly GridViewFactory _viewFactory;
        private SearchPattern _searchPattern;
        
        private GrowingLogLinesCollection? _lines;
        public Action<int>? NavigateToIndexRequested;
        private string _title;
        private  bool _isIndeterminateProgress;

        private readonly object _cancellationTokenSourceLock = new();
        private CancellationTokenSource? _currentSearchCancellationTokenSource;
        private bool _isSearching;
        private double _progress;
        private Regex? _highlightRegex;
        private readonly FilterSettings _filterSettings;
     
        private Indexer? _currentSearchIndexer;
        private int? _currentItemIndex;
        private readonly LogModelFacade _logModelFacade;
        private SearchLineIndex? _currentSearchLineIndex;
        private readonly Selection _markedLines;

        public SearchDocumentViewModel(
            LogModelFacade logModelFacade,
            FilterSettings filterSettings,
            GridViewFactory viewFactory,
            SearchPattern searchPattern,
            Selection markedLines)
        {
            
            _viewFactory = viewFactory;

            _logModelFacade = logModelFacade;
            _title = searchPattern.Pattern;
            
            _filterSettings = filterSettings;
            _filterSettings.ExclusionsChanged += UpdateLines;

            _markedLines = markedLines;
     
            SearchPattern = searchPattern;
            ElementClickedCommand = new DelegateCommand(
                param =>
                {
                    if (param is not ElementClickedEventArgs args)
                        return;
                    var elementIndex = args.ElementIndex;

                    if (_currentItemIndex == elementIndex)                    
                        RequestNavigation(elementIndex);
                });
        }

        private void RequestNavigation(int elementIndex)
        {
            var lineIndex = (Lines?[elementIndex] as LineViewModel)?.Index;
            if (lineIndex is { } index)
                NavigateToIndexRequested?.Invoke(index);
        }

        public string Title => $"{SearchPattern.Pattern} ({Lines?.Count})";

        public NavigateToLineRequest NavigateToLineRequest { get; } = new();

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
                HighlightRegex = _searchPattern.GetRegex(RegexOptions.None);
                StartSearch();
            }
        }

        public int? CurrentItemIndex
        {
            get => _currentItemIndex;
            set
            {
                SetAndRaiseIfChanged(ref _currentItemIndex, value < 0 ? null : value);
                if (_currentItemIndex is not { } currentItemIndex) return;
                RequestNavigation(currentItemIndex);
            }
        }

        public ICommand ElementClickedCommand
        {
            get;
            private set;
        }

        private void StartSearch()
        {
            lock (_cancellationTokenSourceLock)
            {
                if (_currentSearchCancellationTokenSource != null &&
                    _currentSearchCancellationTokenSource.IsCancellationRequested)
                    return;
                
                _currentSearchCancellationTokenSource?.Cancel();
            }

            var newCancellationTokenSource = new CancellationTokenSource();

            SearchProgress = 0;
            IsIndeterminateProgress = true;
            IsSearching = true;
            CurrentItemIndex = null;
            
            var (progress, searchIndexer, searchLineIndex) = Data.Search.Search.CreateSearchIndex(
                _logModelFacade,
                _searchPattern.GetRegex(RegexOptions.Compiled),
                newCancellationTokenSource.Token);

            _currentSearchIndexer = searchIndexer;
            _currentSearchLineIndex = searchLineIndex;

            lock (_cancellationTokenSourceLock)
            {
                _currentSearchCancellationTokenSource = newCancellationTokenSource;
            }

            UpdateLines();
            UpdateDocumentWhileLoading(progress, newCancellationTokenSource.Token);
            InvokePropertyChanged(nameof(Title));
        }

        private GrowingLogLinesCollection GetLineCollection(
            Indexer searchIndexer,          // components -> searchResultLineNumber
            SearchLineIndex searchLineIndex, // searchResultLineNumber -> originalLogLineNumber mapping

            IReadOnlyDictionary<int, IEnumerable<string>> exclusions)
        {
            var lineProvider = _logModelFacade.LineProvider; // originalLogLineNumber -> string 
            var lineParser = _logModelFacade.LineParser;

            var filteredSearchResultLineNumbersProvider = searchIndexer.GetIndexedLinesProvider(exclusions); // collection of filtered searchResultLineNumbers
            // to map from search result line numbers to original line numbers
            var filteredOriginalLineNumbersProvider =
                new ItemProviderMapper<int>(filteredSearchResultLineNumbersProvider, searchLineIndex);

            var filteredLineWithOriginalLineNumber =
                new ItemProviderMapper<(int originalLogLineNumber, string str)>(
                    filteredOriginalLineNumbersProvider, lineProvider);

            var virtualList = 
                new VirtualList<(int originalLogLineNumber, string str), ItemViewModel>(
                    filteredLineWithOriginalLineNumber, indexAndString =>
                        new LineViewModel(indexAndString.originalLogLineNumber, indexAndString.str, lineParser, 
                            _markedLines));

            for (int i = 0; i < Math.Min(virtualList.Count, 10); i++)
            {
                Console.Write(virtualList[i]);
            }

            return new GrowingLogLinesCollection(new List<ItemViewModel>(), virtualList);
        }
        

        private async void UpdateDocumentWhileLoading(Data.Search.Search.Progress progress,
            CancellationToken cancellationToken)
        {
            try
            {
                var delay = 10;
                IsSearching = true;
                while (!progress.IsFinished)
                {
                    var count = Lines?.Count;
                    Lines?.UpdateCount();
                    if (count != Lines?.Count)
                        InvokePropertyChanged(nameof(Title));

                    IsIndeterminateProgress = false;
                    SearchProgress = progress.Value * 100.0;

                    await Task.Delay(delay, cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    if (delay < 150)
                        delay *= 2;
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                Lines?.UpdateCount();
                SearchProgress = progress.Value * 100.0;
                IsSearching = false;
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void UpdateLines()
        {
            if (_currentSearchIndexer == null || _currentSearchLineIndex == null)
                return;
            
            var currentItemIndex = CurrentItemIndex;
            var originalLineIndex = currentItemIndex switch
            {
                { } ind => (Lines?[ind] as LineViewModel)?.Index,
                _ => null
            };

            Lines = GetLineCollection(
                _currentSearchIndexer, _currentSearchLineIndex, _filterSettings.Exclusions);

            if (originalLineIndex is not { } index) return;
            
            var nearestIndex = _currentSearchLineIndex.GetIndexByOriginalIndex(index);
            if (nearestIndex < 0 || nearestIndex >= Lines.Count)
                CurrentItemIndex = null;
            else
                NavigateToLineRequest.Raise(_currentSearchLineIndex.GetIndexByOriginalIndex(index));
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