using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace LogGrokCore
{
    internal class SearchDocumentViewModel : ViewModelBase
    {
        private readonly GridViewFactory _viewFactory;

        public SearchDocumentViewModel(GridViewFactory viewFactory)
        {
            _viewFactory = viewFactory;
            AddToScratchPadCommand = new DelegateCommand(() => throw new NotImplementedException());
        }

        public bool IsIndeterminateProgress { get; private set; }

        public bool IsSearching { get; private set; }

        public double SearchProgress { get; private set; }

        public GrowingLogLinesCollection? Lines { get; private set; }

        public object? SelectedValue { get; private set; }
        
        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand AddToScratchPadCommand { get; private set; }
        
        
    }
}