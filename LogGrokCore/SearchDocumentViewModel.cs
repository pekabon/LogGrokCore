using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore
{
    internal class SearchDocumentViewModel : ViewModelBase
    {
        private readonly GridViewFactory _viewFactory;
        private SearchPattern _searchPattern;

        public SearchDocumentViewModel(GridViewFactory viewFactory, SearchPattern searchPattern)
        {
            _viewFactory = viewFactory;
            _searchPattern = searchPattern;
            AddToScratchPadCommand = new DelegateCommand(() => throw new NotImplementedException());
        }

        public bool IsIndeterminateProgress { get; private set; }

        public bool IsSearching { get; private set; }

        public double SearchProgress { get; private set; }

        public GrowingLogLinesCollection? Lines { get; private set; }

        public object? SelectedValue { get; private set; }
        
        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand AddToScratchPadCommand { get; private set; }

        public void SetSearchPattern(SearchPattern searchPattern)
        {
            _searchPattern = searchPattern;
            StartSearch();
        }

        private void StartSearch()
        {
        }
    }
}