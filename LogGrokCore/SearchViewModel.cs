namespace LogGrokCore
{
    internal class SearchViewModel : ViewModelBase
    {
        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set => SetAndRaiseIfChanged(ref _searchText, value);
        }
    }
}