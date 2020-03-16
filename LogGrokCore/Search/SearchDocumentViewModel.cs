using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore.Search
{
    internal class SearchDocumentViewModel : ViewModelBase
    {
        private readonly GridViewFactory _viewFactory;
        private SearchPattern _searchPattern;

        public SearchDocumentViewModel(GridViewFactory viewFactory, SearchPattern searchPattern)
        {
            _viewFactory = viewFactory;
            _searchPattern = searchPattern;
            Title = searchPattern.Pattern;
            AddToScratchPadCommand = new DelegateCommand(() => throw new NotImplementedException());
        }

        public string Title { get; }

        public bool IsIndeterminateProgress { get; private set; }

        public bool IsSearching { get; private set; }

        public double SearchProgress { get; private set; }

        public GrowingLogLinesCollection? Lines { get; private set; }

        public object? SelectedValue { get; set; }
        
        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand AddToScratchPadCommand { get; private set; }

        public void SetSearchPattern(SearchPattern searchPattern)
        {
            _searchPattern = searchPattern;
            StartSearch().Start();
        }

        private async Task StartSearch()
        {
            Lines = new GrowingLogLinesCollection(() => null,
                new List<ItemViewModel>());

            await Task.Factory.StartNew(() =>
            {
            });
        }
    }
}