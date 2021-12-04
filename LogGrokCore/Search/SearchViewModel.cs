using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;
using LogGrokCore.Controls;

namespace LogGrokCore.Search
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SearchViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly Func<SearchPattern, SearchDocumentViewModel> _searchDocumentViewModelFactory;
        private string _textToSearch = string.Empty;
        private  bool _isCaseSensitive = SearchPattern.Empty.IsCaseSensitive;
        private bool _useRegex = SearchPattern.Empty.UseRegex;

        private DispatcherTimer? _searchPatternThrottleTimer;
        private DispatcherTimer? _autocompletionThrottleTimer;
        
        private readonly TimeSpan _searchPatternCommitThrottleInterval = TimeSpan.FromMilliseconds(500);
        private readonly TimeSpan _autoCompletionThrottleInterval = TimeSpan.FromSeconds(2);

        private SearchPattern _searchPattern = SearchPattern.Empty;
            
        private SearchDocumentViewModel? _currentDocument;
        private readonly SearchAutocompleteCache _searchAutocompleteCache;

        public SearchViewModel(Func<SearchPattern, SearchDocumentViewModel> searchDocumentViewModelFactory,
            SearchAutocompleteCache searchAutocompleteCache)
        {
            _searchDocumentViewModelFactory =
                pattern =>
                {
                    var newDocument = searchDocumentViewModelFactory(pattern);
                    newDocument.NavigateToIndexRequested += i => CurrentLineChanged?.Invoke(i);
                    return newDocument;
                };
                
            ClearSearchCommand = new DelegateCommand(ClearSearch);
            CloseDocumentCommand = DelegateCommand.Create<SearchDocumentViewModel>(CloseDocument);
            AddNewSearchCommand = new DelegateCommand(() => AddNewSearch(_searchPattern.Clone()));
            SearchTextCommand = new DelegateCommand(SearchText, text => !string.IsNullOrEmpty(text as string));
            Activate = new DelegateCommand(() => SetFocusRequest.Invoke());

            Documents = new ObservableCollection<SearchDocumentViewModel>();
            _searchAutocompleteCache = searchAutocompleteCache;
        }
        public event Action<int>? CurrentLineChanged;

        public event Action<Regex>? CurrentSearchChanged;

        public SearchDocumentViewModel? CurrentDocument
        {
            get => _currentDocument;
            set
            {
                if (_currentDocument == value) return;
                SetAndRaiseIfChanged(ref _currentDocument,  value);
                
                if (_currentDocument == null)
                {
                    TextToSearch = string.Empty;
                }
                else
                {
                    var searchPattern = _currentDocument.SearchPattern;
                    TextToSearch = searchPattern.Pattern;
                    IsCaseSensitive = searchPattern.IsCaseSensitive;
                    UseRegex = searchPattern.UseRegex;
                }
            }
        }
        public SetFocusRequest SetFocusRequest { get; } = new(); 

        public string TextToSearch
        {
            get => _textToSearch;
            set => CommitSearchPattern(ref _textToSearch, value, _searchPatternCommitThrottleInterval);
        }

        public bool IsCaseSensitive
        {
            get => _isCaseSensitive;
            set => CommitSearchPattern(ref _isCaseSensitive, value, TimeSpan.Zero);
        }

        public bool UseRegex
        {
            get => _useRegex;
            set
            {
                CommitSearchPattern(ref _useRegex, value, TimeSpan.Zero);
                InvokePropertyChanged(nameof(TextToSearch));
            }
        }

        public bool IsFilterEnabled => !_searchPattern.IsEmpty;

        public bool IsFilterDisabled => _searchPattern.IsEmpty;
        
        public ObservableCollection<SearchDocumentViewModel> Documents { get; }

        public string Error => string.Empty;

        public IEnumerable<string> AutoCompleteList => _searchAutocompleteCache.Items;

        public string this[string columnName] =>
            columnName switch
            {
                nameof(TextToSearch) =>
                    new SearchPattern(TextToSearch, IsCaseSensitive, UseRegex).RegexParseError,
                _ => string.Empty
            };

        public ICommand Activate { get; }
        
        public ICommand SearchTextCommand { get; set; }

        public ICommand ClearSearchCommand { get; }

        public ICommand CloseDocumentCommand { get; }

        public ICommand AddNewSearchCommand { get; }

        private void CommitSearchPattern<T>(ref T field, T newValue, TimeSpan timeSpan, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, newValue)) return;
            SetAndRaiseIfChanged(ref field, newValue, propertyName);
            Throttle(ref _searchPatternThrottleTimer, 
                () =>
                {
                    CommitSearchPatternImmediately(TextToSearch, IsCaseSensitive, UseRegex);
                    Throttle(ref _autocompletionThrottleTimer, () => _searchAutocompleteCache.Add(TextToSearch),
                        _autoCompletionThrottleInterval);
                },
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
            throttleTimer.Tick += (_, _) =>
            {
                timer.Stop();
                timer = null;
                action();
            };
            throttleTimer.Start();
        }

        private void CommitSearchPatternImmediately(string searchText, in bool isCaseSensitive, in bool useRegex)
        {
            var newSearchPattern = new SearchPattern(searchText, isCaseSensitive, useRegex);
            if (!newSearchPattern.IsValid)
                return;
            
            _searchPattern = newSearchPattern;
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
                var newDocument = _searchDocumentViewModelFactory(_searchPattern);
                Documents.Add(newDocument);
                CurrentDocument = newDocument;
            }
        }

        private void ClearSearch()
        {
            TextToSearch = string.Empty;
            CommitSearchPatternImmediately(TextToSearch, IsCaseSensitive, UseRegex);
        }

        private void CloseDocument(SearchDocumentViewModel searchDocumentViewModel)
        {
            searchDocumentViewModel.Dispose();
            if (Documents.Count != 0) return;
            TextToSearch = string.Empty;
            CurrentDocument = null;
        }

        private void AddNewSearch(SearchPattern searchPattern)
        {
            var newDocument = _searchDocumentViewModelFactory(searchPattern);
            Documents.Add(newDocument);
            CurrentDocument = newDocument;
        }
        
        private void SearchText(object obj)
        {
            if (obj is not string text) return;
            var searchPattern = new SearchPattern(text, false, false);
            AddNewSearch(searchPattern);
        }
    }
}