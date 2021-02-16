using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Controls;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Data;
using LogGrokCore.Data.Virtualization;
using LogGrokCore.Filter;

namespace LogGrokCore
{
    internal class LogViewModel : ViewModelBase
    {
        private readonly GridViewFactory _viewFactory;
        private double _progress;
        private readonly LogModelFacade _logModelFacade;
        private bool _isLoading;
        private IEnumerable? _selectedItems;
        private readonly LineViewModelCollectionProvider _lineViewModelCollectionProvider;
        private GrowingLogLinesCollection? _lines;
        private readonly Lazy<int> _headerLineCount;
        private Regex? _highlightRegex;
        private Func<int, int> _getIndexByValue = x => x;
        private readonly FilterSettings _filterSettings;
        private int _currentItemIndex;

        public LogViewModel(
            LogModelFacade logModelFacade,
            LineViewModelCollectionProvider lineViewModelCollectionProvider,
            GridViewFactory viewFactory,
            HeaderProvider headerProvider,
            FilterSettings filterSettings)
        {
            _headerLineCount = new Lazy<int>(() => headerProvider.Header == null ? 0 : 1);
            _logModelFacade = logModelFacade;
            _filterSettings = filterSettings;

            var lineProvider = _logModelFacade.LineProvider;
            var lineParser = _logModelFacade.LineParser;
            
            _lineViewModelCollectionProvider = lineViewModelCollectionProvider;
            filterSettings.ExclusionsChanged += () =>
            {
                InvokePropertyChanged(nameof(HaveFilter));
                UpdateFilteredCollection();
            };

            var lineCollection =
                new VirtualList<(int index, string str), ItemViewModel>(lineProvider,
                    (indexAndString) => 
                        new LineViewModel(indexAndString.index, indexAndString.str, lineParser));
            
            Lines = new GrowingLogLinesCollection(() => headerProvider.Header, lineCollection);
            
            CopyPathToClipboardCommand =
                new DelegateCommand(() => TextCopy.ClipboardService.SetText(_logModelFacade.LogFile.FilePath));
            OpenContainingFolderCommand = new DelegateCommand(OpenContainingFolder);

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
            
            ClearExclusionsCommand = new DelegateCommand(() =>
                {
                    _filterSettings.ClearAllExclusions();
                });

            CopySelectedItemsToClipboardCommand = new DelegateCommand(CopySelectedItemsToClipboard, 
                () => SelectedItems?.Cast<object>().Any() ?? false); 
            
            _viewFactory = viewFactory;
            UpdateDocumentWhileLoading();
            UpdateProgress();
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

        private void UpdateFilteredCollection()
        {
            var currentItemIndex = CurrentItemIndex;
            var item = Lines?[currentItemIndex];
            var originalLineIndex = (item as LineViewModel)?.Index; 

            var (lineViewModelCollection, getIndexByValue) = 
                _lineViewModelCollectionProvider.GetLogLinesCollection(_logModelFacade.Indexer, 
                    _filterSettings.Exclusions);

            _getIndexByValue = getIndexByValue;
            Lines = lineViewModelCollection;

            if (originalLineIndex is {} index)
            {
                NavigateTo(index);
            }
        }

        public int CurrentItemIndex
        {
            get => _currentItemIndex;
            set => SetAndRaiseIfChanged(ref _currentItemIndex, value);
        }

        public bool CanFilter => true;

        public bool HaveFilter => _filterSettings.HaveExclusions;

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

        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand CopyPathToClipboardCommand { get; }
        
        public ICommand OpenContainingFolderCommand { get; }
        
        public NavigateToLineRequest NavigateToLineRequest { get; } = new();
        
        public GrowingLogLinesCollection? Lines
        {
            get => _lines;
            private set => SetAndRaiseIfChanged(ref _lines, value);
        }

        private IEnumerable<string> GetComponentsInSelectedLines(int componentIndex)
        {
            var lineViewModels = SelectedItems?.OfType<LineViewModel>() ?? Enumerable.Empty<LineViewModel>();
            return lineViewModels.Select(line => line[componentIndex]).Distinct();
        }

        private async void UpdateDocumentWhileLoading()
        {
            var delay = 10;
            IsLoading = true;
            while (!_logModelFacade.IsLoaded)
            {
                await Task.Delay(delay);
                if (delay < 500)
                    delay *= 2;
                Lines?.UpdateCount();
            }

            Lines?.UpdateCount();
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

        private void OpenContainingFolder()
        {
            var filePath = _logModelFacade.LogFile.FilePath;

            var cmdLine = File.Exists(filePath)
                ? $"/select, {filePath}"
                : $"/select, {Directory.GetParent(filePath)?.FullName}";

            _ = Process.Start("explorer.exe", cmdLine);
        }

        public void NavigateTo(in int logLineNumber)
        {
            NavigateToLineRequest.Raise(_getIndexByValue(logLineNumber) + _headerLineCount.Value);
        }
    }
}