using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Controls;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Controls.ListControls;
using LogGrokCore.Data;
using LogGrokCore.Filter;

namespace LogGrokCore
{
    public class LogViewModel : ViewModelBase
    {
        private readonly GridViewFactory _viewFactory;
        private double _progress;
        private readonly LogModelFacade _logModelFacade;
        private bool _isLoading;
        private IEnumerable? _selectedItems;
        private readonly LineViewModelCollectionProvider _lineViewModelCollectionProvider;
        private Regex? _highlightRegex;
        private Func<int, int> _getIndexByValue;
        private readonly FilterSettings _filterSettings;
        private int _currentItemIndex;
        private readonly IReadOnlyList<ItemViewModel> _headerCollection;

        public LogViewModel(
            LogModelFacade logModelFacade,
            LineViewModelCollectionProvider lineViewModelCollectionProvider,
            GridViewFactory viewFactory,
            FilterSettings filterSettings,
            ColumnSettings columnSettings)
        {
            _logModelFacade = logModelFacade;
            _filterSettings = filterSettings;
            
            var lineProvider = _logModelFacade.LineProvider;
            var lineParser = _logModelFacade.LineParser;
            
            _lineViewModelCollectionProvider = lineViewModelCollectionProvider;
            filterSettings.ExclusionsChanged += UpdateFilteredCollection;

            IReadOnlyList<ItemViewModel> lineCollection;
            (_headerCollection, lineCollection, _getIndexByValue) 
                = lineViewModelCollectionProvider.GetLogLinesCollection(lineProvider);
            
            Lines = new GrowingLogLinesCollection(_headerCollection, lineCollection);
            
            ExcludeCommand = DelegateCommand.Create(
                    (int componentIndex) => {
                        _filterSettings.AddExclusions(
                            logModelFacade.MetaInformation.GetIndexedFieldIndexByFieldIndex(componentIndex),
                            GetComponentsInSelectedLines(componentIndex));
                    });    
            
            ExcludeAllButCommand = DelegateCommand.Create(
                (int componentIndex) =>
                {
                    
                    _filterSettings.ExcludeAllExcept(
                        logModelFacade.MetaInformation.GetIndexedFieldIndexByFieldIndex(componentIndex),
                        GetComponentsInSelectedLines(componentIndex));
                });
            
            ClearExclusionsCommand = new DelegateCommand(() => _filterSettings.ClearAllExclusions(),
                () => _filterSettings.HaveExclusions);

            CopySelectedItemsToClipboardCommand = new DelegateCommand(CopySelectedItemsToClipboard, 
                () => SelectedItems?.Cast<object>().Any() ?? false); 
            
            _viewFactory = viewFactory;
            ColumnSettings = columnSettings;
            UpdateDocumentWhileLoading();
            UpdateProgress();
        }

        public ColumnSettings ColumnSettings
        {
            get;
        }

        public int CurrentItemIndex
        {
            get => _currentItemIndex;
            set => SetAndRaiseIfChanged(ref _currentItemIndex, value);
        }

        public bool CanFilter => true;

        public LogMetaInformation MetaInformation => _logModelFacade.MetaInformation;

        public ICommand CopySelectedItemsToClipboardCommand { get; }
        
        public ICommand ExcludeCommand { get; }

        public ICommand ExcludeAllButCommand { get; }
        
        public ICommand ClearExclusionsCommand { get; }

        public IEnumerable? SelectedItems
        {
            get => _selectedItems;
            set
            {
                if (_selectedItems == null && value == null) return;
                if (_selectedItems != null && value != null && 
                    _selectedItems.Cast<object>().SequenceEqual(value.Cast<object>())) return;
                _selectedItems = value;
                InvokePropertyChanged();
            }
        }

        public Regex? HighlightRegex
        {
            get => _highlightRegex;
            set => SetAndRaiseIfChanged(ref _highlightRegex, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetAndRaiseIfChanged(ref _progress, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetAndRaiseIfChanged(ref _isLoading, value);
        }

        public ViewBase CustomView => _viewFactory.CreateView(ColumnSettings.ColumnWidths);

        public NavigateToLineRequest NavigateToLineRequest { get; } = new();
        
        public GrowingLogLinesCollection Lines { get; }

        public void NavigateTo(in int logLineNumber)
        {
            NavigateToLineRequest.Raise(_getIndexByValue(logLineNumber) + _headerCollection.Count);
        }
        
        private IEnumerable<string> GetComponentsInSelectedLines(int componentIndex)
        {
            var lineViewModels = SelectedItems?.OfType<LineViewModel>() ?? Enumerable.Empty<LineViewModel>();
            return lineViewModels.Select(line => line[componentIndex].OriginalText ?? string.Empty).Distinct();
        }

        private async void UpdateDocumentWhileLoading()
        {
            var delay = 10;
            IsLoading = true;
            while (!_logModelFacade.IsLoaded)
            {
                Lines.UpdateCount();
                await Task.Delay(delay);
                if (delay < 500)
                    delay *= 2;
            }

            Lines.UpdateCount();
            IsLoading = false;
        }

        private async void UpdateProgress()
        {
            while (!_logModelFacade.IsLoaded)
            {
                await Task.Delay(200);
                Progress = _logModelFacade.LoadProgress;
            }

            Progress = 100;
        }
        
        private void CopySelectedItemsToClipboard()
        {
            if (SelectedItems == null) return;
            var orderedLines =
                SelectedItems.OfType<LogHeaderViewModel>().Select(h => h.ToString())
                    .Concat(SelectedItems.OfType<LineViewModel>().OrderBy(ln => ln.Index).Select(ln => ln.ToString()));
            
            var  text = new StringBuilder();
            foreach (var line in orderedLines)
            {
                _ = text.Append(line);
                _ = text.Append("\r\n");
            }
            _ = text.Replace("\0", string.Empty);

            TextCopy.ClipboardService.SetText(text.ToString());
        }

        private async void UpdateFilteredCollection()
        {
            var currentItemIndex = CurrentItemIndex;
            var item = Lines[currentItemIndex];
            var originalLineIndex = (item as LineViewModel)?.Index;

            var exclusionsCopy = _filterSettings.Exclusions.ToDictionary(kv 
                => kv.Key, kv => kv.Value); 
            var (headerCollection, linesCollection, getIndexByValue) 
                = await Task.Factory.StartNew(() => _lineViewModelCollectionProvider.GetLogLinesCollection(
                    _logModelFacade.Indexer,
                    _filterSettings.Exclusions));

            var newExclusionsCopy = _filterSettings.Exclusions.ToList();
            if (!exclusionsCopy.SequenceEqual(newExclusionsCopy))
            {
                return;
            }
            
            _getIndexByValue = getIndexByValue;
            Lines.Reset(headerCollection, linesCollection);

            if (originalLineIndex is { } index)
            {
                NavigateTo(index);
            }
        }
    }
}