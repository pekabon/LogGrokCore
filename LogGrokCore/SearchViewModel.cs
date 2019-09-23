using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;

namespace LogGrokCore
{
    internal struct SearchPattern
    {
        public SearchPattern(string searchText, in bool isCaseSensitive, in bool useRegex)
        {
            Pattern = searchText;
            IsCaseSensitive = isCaseSensitive;
            UseRegex = useRegex;
        }

        public string Pattern { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool UseRegex { get; set; }

        public Regex GetRegex(RegexOptions regexAdditionalOptions)
        {
            var regexOptions = IsCaseSensitive ? RegexOptions.None | RegexOptions.IgnoreCase : RegexOptions.None;
            var pattern = UseRegex ? Pattern : Regex.Escape(Pattern);
            return new Regex(pattern, regexOptions | regexAdditionalOptions);
        }
    }
    internal class SearchViewModel : ViewModelBase
    {
        private readonly Func<SearchPattern, SearchDocumentViewModel> _searchDocumentViewModelFactory;
        private string _searchText = string.Empty;
        private  bool _isCaseSensitive;
        private bool _useRegex;

        private DispatcherTimer? _searchPatternThrottleTimer;
        private SearchPattern _searchPattern = new SearchPattern(string.Empty, false, false);

        public SearchViewModel(Func<SearchPattern, SearchDocumentViewModel> searchDocumentViewModelFactory)
        {
            _searchDocumentViewModelFactory = searchDocumentViewModelFactory;
            ClearSearchCommand = new DelegateCommand(() => throw new NotImplementedException());
            CloseDocumentCommand = new DelegateCommand(() => throw new NotImplementedException());
            AddNewSearchCommand = new DelegateCommand(() => throw new NotImplementedException());
            Documents = new ObservableCollection<SearchDocumentViewModel>();
        }

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

        public ICommand ClearSearchCommand { get; private set; }

        public ICommand CloseDocumentCommand { get; private set; }

        public ICommand AddNewSearchCommand { get; private set; }
        
        public SearchDocumentViewModel? CurrentDocument { get; set; }

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
            if (CurrentDocument != null)
            {
                CurrentDocument.SetSearchPattern(_searchPattern);
            }
            else
            {
                CurrentDocument = _searchDocumentViewModelFactory(_searchPattern);
                Documents.Add(CurrentDocument);
            }
        }

        public ObservableCollection<SearchDocumentViewModel> Documents { get; private set; }
    }
}