namespace LogGrokCore
{
    internal class DocumentViewModel : ViewModelBase
    {
        private bool _isCurrentDocument;

        public DocumentViewModel(LogViewModel logViewModel, SearchViewModel searchViewModel)
        {
            LogViewModel = logViewModel;
            SearchViewModel = searchViewModel;
        }

        public LogViewModel LogViewModel { get; }

        public SearchViewModel SearchViewModel { get; }

        public bool IsCurrentDocument
        {
            get => _isCurrentDocument;
            set => SetAndRaiseIfChanged(ref _isCurrentDocument, value);
        }
    }
}