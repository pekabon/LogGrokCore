using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;

namespace LogGrokCore.Search
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SearchViewModel : ViewModelBase
    {
        private readonly Func<SearchPattern, SearchDocumentViewModel> _searchDocumentViewModelFactory;
        private string _searchText = string.Empty;
        private  bool _isCaseSensitive;
        private bool _useRegex;

        private DispatcherTimer? _searchPatternThrottleTimer;
        private SearchPattern _searchPattern = new(string.Empty, false, false);
        private SearchDocumentViewModel? _currentDocument;
        public SearchViewModel(Func<SearchPattern, SearchDocumentViewModel> searchDocumentViewModelFactory)
        {
            _searchDocumentViewModelFactory =
                pattern =>
                {
                    var newDocument = searchDocumentViewModelFactory(pattern);
                    newDocument.SelectedIndexChanged += i => CurrentLineChanged?.Invoke(i);
                    return newDocument;
                };
                
            ClearSearchCommand = new DelegateCommand(ClearSearch);
            CloseDocumentCommand = DelegateCommand.Create<SearchDocumentViewModel>(CloseDocument);
            AddNewSearchCommand = new DelegateCommand(AddNewSearch);
            Documents = new ObservableCollection<SearchDocumentViewModel>();
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
            CommitSearchPatternImmediately(SearchText, IsCaseSensitive, UseRegex);
        }

        private void CloseDocument(SearchDocumentViewModel  d)
        {
            if (Documents.Count != 0) return;
            SearchText = string.Empty;
            CurrentDocument = null;
        }

        private void AddNewSearch()
        {
            var newDocument = _searchDocumentViewModelFactory(_searchPattern.Clone());
            Documents.Add(newDocument);
            CurrentDocument = newDocument;
        }

        public event Action<int>? CurrentLineChanged;

        public event Action<Regex>? CurrentSearchChanged;

        public string SearchText
        {
            get => _searchText;
            set => CommitSearchPattern(ref _searchText, value, TimeSpan.FromMilliseconds(500)); 
        }

        public bool IsCaseSensitive
        {
            get => _isCaseSensitive;
            set => CommitSearchPattern(ref _isCaseSensitive, value, TimeSpan.Zero);
        }

        public bool UseRegex
        {
            get => _useRegex;
            set => CommitSearchPattern(ref _useRegex, value, TimeSpan.Zero);
        }

        public bool IsFilterEnabled
        {
            get => !_searchPattern.IsEmpty;
        }

        public bool IsFilterDisabled
        {
            get => _searchPattern.IsEmpty;
        }

        
        public ICommand ClearSearchCommand { get; private set; }

        public ICommand CloseDocumentCommand { get; private set; }

        public ICommand AddNewSearchCommand { get; private set; }

        public SearchDocumentViewModel? CurrentDocument
        {
            get => _currentDocument;
            set
            {
                if (_currentDocument == value) return;
                SetAndRaiseIfChanged(ref _currentDocument,  value);

                
                if (_currentDocument == null)
                {
                    SearchText = string.Empty;
                    IsCaseSensitive = false;
                    UseRegex = false;
                }
                else
                {
                    var searchPattern = _currentDocument.SearchPattern;
                    SearchText = searchPattern.Pattern;
                    IsCaseSensitive = searchPattern.IsCaseSensitive;
                    UseRegex = searchPattern.UseRegex;
                }
            }
        }

        private void CommitSearchPattern<T>(ref T field, T newValue, TimeSpan timeSpan, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, newValue)) return;
            SetAndRaiseIfChanged(ref field, newValue, propertyName);
            Throttle(ref _searchPatternThrottleTimer, 
                () => CommitSearchPatternImmediately(SearchText, IsCaseSensitive, UseRegex), 
                timeSpan);
        }

        private static void Throttle(ref DispatcherTimer? throttleTimer, Action action, TimeSpan interval)
        {
            throttleTimer?.Stop();
            throttleTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher.CurrentDispatcher)
            {
                Interval = interval
            };

            DispatcherTimer? timer = throttleTimer;
            throttleTimer.Tick += (o, e) =>
            {
                timer.Stop();
                timer = null;
                action();
            };
            throttleTimer.Start();
        }

        private void CommitSearchPatternImmediately(string searchText, in bool isCaseSensitive, in bool useRegex)
        {
            _searchPattern = new SearchPattern(searchText, isCaseSensitive, useRegex);
            CurrentSearchChanged?.Invoke(_searchPattern.GetRegex(RegexOptions.None));

            InvokePropertyChanged(nameof(IsFilterEnabled));
            InvokePropertyChanged(nameof(IsFilterDisabled));
            
            if (_searchPattern.IsEmpty)
            {
                if (CurrentDocument != null)
                {
                    CurrentDocument.Dispose();
                    Documents.Remove(CurrentDocument);
                    CurrentDocument = null;
                    return;
                }
            }

            if (CurrentDocument != null)
            {
                CurrentDocument.SearchPattern = _searchPattern;
            }
            else
            {
                var newDocument = _searchDocumentViewModelFactory(_searchPattern);;
                Documents.Add(newDocument);
                CurrentDocument = newDocument;
            }
        }

        public ObservableCollection<SearchDocumentViewModel> Documents { get; }
    }
}