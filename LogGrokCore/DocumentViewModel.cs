using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Data;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    internal class DocumentViewModel : INotifyPropertyChanged
    {
        private readonly GridViewFactory _viewFactory;
        private double _progress;
        private readonly LogModelFacade _logModelFacade;
        private bool _isLoading;
        private bool _isCurrentDocument;

        public DocumentViewModel(
            LogModelFacade logModelFacade,
            GridViewFactory viewFactory)
        {
            _logModelFacade = logModelFacade;

            Title = Path.GetFileName(_logModelFacade.FilePath);
            
            
            var lineProvider = _logModelFacade.LineProvider;
            var lineParser = _logModelFacade.LineParser;
            var lineCollection =
                new VirtualList<string, LineViewModel>(lineProvider,
                    (str, index) => new LineViewModel(index, str, lineParser));
            Lines = new GrowingCollectionAdapter<LineViewModel>(lineCollection);

            CopyPathToClipboardCommand =
                new DelegateCommand(() => TextCopy.Clipboard.SetText(_logModelFacade.FilePath));
            OpenContainingFolderCommand = new DelegateCommand(OpenContainingFolder);

            _viewFactory = viewFactory;
            UpdateDocumentWhileLoading();
            UpdateProgress();
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

        public GrowingCollectionAdapter<LineViewModel> Lines { get; }

        public bool IsCurrentDocument
        {
            get => _isCurrentDocument;
            set => SetAndRaiseIfChanged(ref _isCurrentDocument, value);
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
                Lines.UpdateCount();
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

        private void OpenContainingFolder()
        {
            var filePath = _logModelFacade.FilePath;

            var cmdLine = File.Exists(filePath)
                ? $"/select, {filePath}"
                : $"/select, {Directory.GetParent(filePath).FullName}";

            _ = Process.Start("explorer.exe", cmdLine);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetAndRaiseIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}