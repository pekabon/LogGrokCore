using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using LogGrokCore.Colors;
using LogGrokCore.Controls;
using LogGrokCore.Data;
using LogGrokCore.MarkedLines;
using LogGrokCore.Search;

namespace LogGrokCore
{
    internal class DocumentViewModel : ViewModelBase
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
            Title = 
                Path.GetFileName(logModelFacade.LogFile.FilePath) 
                    ?? throw new InvalidOperationException($"Invalid path: {logModelFacade.LogFile.FilePath}");

            LogViewModel = logViewModel;
            SearchViewModel = searchViewModel;
            ColorSettings = colorSettings;
            
            SearchViewModel.CurrentLineChanged += lineNumber => LogViewModel.NavigateTo(lineNumber);
            SearchViewModel.CurrentSearchChanged += regex => LogViewModel.HighlightRegex = regex;

            _markedLines = markedLines;
            _lineProvider = lineProvider;
            _markedLines.Changed += () => MarkedLinesChanged?.Invoke();
        }

        public event Action? MarkedLinesChanged;
        
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

        public string Title { get; }

        public LogViewModel LogViewModel { get; }

        public SearchViewModel SearchViewModel { get; }

        public ColorSettings ColorSettings { get; }
        
        public bool IsCurrentDocument
        {
            get => _isCurrentDocument;
            set => SetAndRaiseIfChanged(ref _isCurrentDocument, value);
        }
    }
}