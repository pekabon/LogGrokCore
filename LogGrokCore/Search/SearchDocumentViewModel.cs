using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Data;
using LogGrokCore.Data.Search;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Search
{
    internal class SearchDocumentViewModel : ViewModelBase, IDisposable
    {
        private readonly GridViewFactory _viewFactory;
        private SearchPattern _searchPattern;
        private readonly LogFile _logFile;
        private readonly LineIndex _lineIndex;
        private readonly ILineParser _lineParser;
        private GrowingLogLinesCollection? _lines;
        public Action<int>? SelectedIndexChanged;
        private object? _selectedValue;
        private string _title;
        private  bool _isIndeterminateProgress;

        private readonly object _cancellationTokenSourceLock = new object();
        private CancellationTokenSource? _currentSearchCancellationTokenSource;
        private bool _isSearching;
        private double _progress;
        private Regex? _highlightRegex;

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
            _title = searchPattern.Pattern;
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
                if (_selectedValue is LineViewModel lineViewModel)
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
            
            var (progress, searchLineIndex) = Data.Search.Search.CreateSearchIndex(
                _logFile.OpenForSequentialRead(),
                _logFile.Encoding,
                _lineIndex,
                _searchPattern.GetRegex(RegexOptions.Compiled),
                _currentSearchCancellationTokenSource.Token);
            
            var lineProvider =  new SearchLineProvider(searchLineIndex, _logFile);
            var lineCollection =
                new VirtualList<(int, string), ItemViewModel>(lineProvider,
                    (value) =>
                    {
                        var (originalIndex, str) = value;
                        return new LineViewModel(originalIndex, str, _lineParser);
                    });

            Lines = new GrowingLogLinesCollection(() => null,
                lineCollection);


            var context = SynchronizationContext.Current;

            var updateScheduled = 0;
            void UpdateCount(Data.Search.Search.Progress progress, bool force)
            {
                if (force)
                {
                    context?.Post(n => { Lines?.UpdateCount(); }, null);
                } 
                else if (0 == Interlocked.CompareExchange(ref updateScheduled, 1, 0))
                {
                    context?.Post(n =>
                    {
                        Lines?.UpdateCount();
                        Task.Delay(TimeSpan.FromMilliseconds(150)).ContinueWith(t =>
                            Interlocked.Exchange(ref updateScheduled, 0));
                        
                    }, null);
                }

                SearchProgress = progress.Value * 100.0;
                IsSearching = !progress.IsFinished;
                IsIndeterminateProgress = false;
            }

            progress.Changed += _ => UpdateCount(progress, false);
            progress.IsFinishedChanged += () => UpdateCount(progress, true);
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