using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Controls;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Data;
using LogGrokCore.Data.Virtualization;

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
        public LogViewModel(
            LogModelFacade logModelFacade,
            LineViewModelCollectionProvider lineViewModelCollectionProvider,
            GridViewFactory viewFactory,
            HeaderProvider headerProvider)
        {
            _headerLineCount = new Lazy<int>(() => headerProvider.Header == null ? 0 : 1);
            _logModelFacade = logModelFacade;

            var lineProvider = _logModelFacade.LineProvider;
            var lineParser = _logModelFacade.LineParser;
            
            _lineViewModelCollectionProvider = lineViewModelCollectionProvider;
            _lineViewModelCollectionProvider.ExclusionsChanged += () => { InvokePropertyChanged(nameof(HaveFilter)); };
            var lineCollection =
                new VirtualList<(int index, string str), ItemViewModel>(lineProvider,
                    (indexAndString) => 
                        new LineViewModel(indexAndString.index, indexAndString.str, lineParser));
            
            Lines = new GrowingLogLinesCollection(() => headerProvider.Header, lineCollection);
            
            CopyPathToClipboardCommand =
                new DelegateCommand(() => TextCopy.ClipboardService.SetText(_logModelFacade.FilePath));
            OpenContainingFolderCommand = new DelegateCommand(OpenContainingFolder);

            void UpdateFilteredCollection(LineViewModelCollectionProvider viewModelCollectionProvider)
            {
                var (lineViewModelCollection,
                    getIndexByValue) = lineViewModelCollectionProvider.GetLogLinesCollection();
                Lines = lineViewModelCollection;
                _getIndexByValue = getIndexByValue;
            }

            ExcludeCommand = DelegateCommand.Create(
                    (int componentIndex) =>
                    {
                        _lineViewModelCollectionProvider.AddExclusions(componentIndex,
                            GetComponentsInSelectedLines(componentIndex));
                        UpdateFilteredCollection(_lineViewModelCollectionProvider);
                    });    
            
            ExcludeAllButCommand = DelegateCommand.Create(
                (int componentIndex) =>
                {
                    _lineViewModelCollectionProvider.ExcludeAllExcept(componentIndex,
                        GetComponentsInSelectedLines(componentIndex));
                    UpdateFilteredCollection(_lineViewModelCollectionProvider);
                });
            
            ClearExclusionsCommand = new DelegateCommand(() =>
                {
                    _lineViewModelCollectionProvider.ClearAllExclusions();
                    UpdateFilteredCollection(_lineViewModelCollectionProvider);
                }
            );
            
            _viewFactory = viewFactory;
            UpdateDocumentWhileLoading();
            UpdateProgress();
        }

        public bool CanFilter => true;

        public bool HaveFilter => _lineViewModelCollectionProvider.HaveExclusions;

        public LogMetaInformation MetaInformation => _logModelFacade.MetaInformation;

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
        
        public NavigateToLineRequest NavigateToLineRequest { get; } = new NavigateToLineRequest();
        
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
            var filePath = _logModelFacade.FilePath;

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