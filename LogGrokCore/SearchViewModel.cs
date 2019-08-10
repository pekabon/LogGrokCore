using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LogGrokCore
{
    internal class SearchViewModel : ViewModelBase
    {
        private string _searchText = string.Empty;

        public SearchViewModel()
        {
            ClearSearchCommand = new DelegateCommand(() => throw new NotImplementedException());
            CloseDocumentCommand = new DelegateCommand(() => throw new NotImplementedException());
            AddNewSearchCommand = new DelegateCommand(() => throw new NotImplementedException());
            Documents = new ObservableCollection<SearchDocumentViewModel>();
        }

        public string SearchText
        {
            get => _searchText;
            set => SetAndRaiseIfChanged(ref _searchText, value);
        }

        public bool IsCaseSensitive { get; private set; }

        public bool UseRegex { get; private set; }

        public ICommand ClearSearchCommand { get; private set; }

        public ICommand CloseDocumentCommand { get; private set; }

        public ICommand AddNewSearchCommand { get; private set; }
        
        public object? CurrentDocument { get; private set; }

        public ObservableCollection<SearchDocumentViewModel> Documents { get; private set; }
    }
}