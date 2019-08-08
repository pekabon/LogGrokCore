using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
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
        
        public LogViewModel(
            LogModelFacade logModelFacade,
            LineViewModelCollectionProvider lineViewModelCollectionProvider,
            GridViewFactory viewFactory,
            HeaderProvider headerProvider)
        {
            _logModelFacade = logModelFacade;

            Title = 
                Path.GetFileName(_logModelFacade.FilePath) 
                ?? throw new InvalidOperationException($"Invalid path: {_logModelFacade.FilePath}");
            
            var lineProvider = _logModelFacade.LineProvider;
            var lineParser = _logModelFacade.LineParser;

            _lineViewModelCollectionProvider = lineViewModelCollectionProvider;
            _lineViewModelCollectionProvider.ExclusionsChanged += () => { InvokePropertyChanged(nameof(HaveFilter)); };
            var lineCollection =
                new VirtualList<string, ItemViewModel>(lineProvider,
                    (str, index) => new LineViewModel(index, str, lineParser));
            Lines = new GrowingLogLinesCollection(headerProvider, lineCollection);

            CopyPathToClipboardCommand =
                new DelegateCommand(() => TextCopy.Clipboard.SetText(_logModelFacade.FilePath));
            OpenContainingFolderCommand = new DelegateCommand(OpenContainingFolder);
            
            ExcludeCommand = DelegateCommand.Create(
                    (int componentIndex) =>
                    {
                        _lineViewModelCollectionProvider.AddExclusions(componentIndex,
                            GetComponentsInSelectedLines(componentIndex));
                        Lines = _lineViewModelCollectionProvider.GetLogLinesCollection();
                    });    
            
            ExcludeAllButCommand = DelegateCommand.Create(
                (int componentIndex) =>
                {
                    _lineViewModelCollectionProvider.ExcludeAllExcept(componentIndex,
                        GetComponentsInSelectedLines(componentIndex));
                    Lines = _lineViewModelCollectionProvider.GetLogLinesCollection();
                });
            
            ClearExclusionsCommand = new DelegateCommand(() =>
                {
                    _lineViewModelCollectionProvider.ClearAllExclusions();
                    Lines = _lineViewModelCollectionProvider.GetLogLinesCollection();
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
        public string Title { get; }

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
                : $"/select, {Directory.GetParent(filePath).FullName}";

            _ = Process.Start("explorer.exe", cmdLine);
        }
    }
}