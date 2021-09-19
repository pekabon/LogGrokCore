using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using LogGrokCore.Colors;
using LogGrokCore.Controls;
using LogGrokCore.Data;
using LogGrokCore.Search;

namespace LogGrokCore
{
    public class DocumentViewModel : ViewModelBase
    {
        private readonly Selection _markedLines;
        private bool _isCurrentDocument;
        private LineProvider _lineProvider;
        private ObservableCollection<(int number, string text)> _markedLineViewModels = new();

        public DocumentViewModel(
            LineProvider lineProvider,
            LogModelFacade logModelFacade,
            LogViewModel logViewModel, 
            SearchViewModel searchViewModel,
            Selection markedLines,
            ColorSettings colorSettings)
        {
            var logFileFilePath = logModelFacade.LogFile.FilePath;
            
            Title = 
                Path.GetFileName(logFileFilePath) 
                    ?? throw new InvalidOperationException($"Invalid path: {logFileFilePath}");

            LogViewModel = logViewModel;
            SearchViewModel = searchViewModel;
            ColorSettings = colorSettings;
            
            SearchViewModel.CurrentLineChanged += lineNumber => NavigateTo(lineNumber);
            SearchViewModel.CurrentSearchChanged += regex => LogViewModel.HighlightRegex = regex;

            _markedLines = markedLines;
            _lineProvider = lineProvider;
            _markedLines.Changed += () => MarkedLinesChanged?.Invoke();
            
            CopyPathToClipboardCommand =
                new DelegateCommand(() => TextCopy.ClipboardService.SetText(logFileFilePath));
            OpenContainingFolderCommand = new DelegateCommand(() => OpenContainingFolder(logFileFilePath));
            DocumentId = logFileFilePath;
        }

        public string DocumentId { get; }
    
        public event Action? MarkedLinesChanged;
        
        public ICommand CopyPathToClipboardCommand { get; }
        
        public ICommand OpenContainingFolderCommand { get; }

        public void NavigateTo(int lineNumber)
        {
            LogViewModel.NavigateTo(lineNumber);
        }
        
        public ObservableCollection<(int number, string text)> MarkedLineViewModels
        {
            get
            {
                var lineNumbers = _markedLines.ToList();
                lineNumbers.Sort();
                var collection = new ObservableCollection<(int number, string text)>();
                foreach (var lineNumber in lineNumbers)
                {
                    var lines = new (int, string)[1];
                    _lineProvider.Fetch(lineNumber, lines.AsSpan());
                    collection.Add((lineNumber, lines[0].Item2));
                }

                return collection;
            }
        }

        public Selection MarkedLines => _markedLines;        
        
        public string Title { get; }

        public LogViewModel LogViewModel { get; }

        public SearchViewModel SearchViewModel { get; }

        public ColorSettings ColorSettings { get; }
        
        public bool IsCurrentDocument
        {
            get => _isCurrentDocument;
            set => SetAndRaiseIfChanged(ref _isCurrentDocument, value);
        }
        
        private void OpenContainingFolder(string path)
        {
            var filePath = path;

            var cmdLine = File.Exists(filePath)
                ? $"/select, {filePath}"
                : $"/select, {Directory.GetParent(filePath)?.FullName}";

            _ = Process.Start("explorer.exe", cmdLine);
        }
    }
}