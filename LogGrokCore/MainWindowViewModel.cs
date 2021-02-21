using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using LogGrokCore.MarkedLines;
using Microsoft.Win32;

namespace LogGrokCore
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private DocumentViewModel? _currentDocument;
        private readonly ApplicationSettings _applicationSettings;

        public ObservableCollection<DocumentViewModel> Documents { get; }

        public MarkedLinesViewModel MarkedLinesViewModel { get; }
        
        public ICommand OpenFileCommand => new DelegateCommand(OpenFile);
        
        public ICommand DropCommand => new DelegateCommand(
            obj=> OpenFiles((IEnumerable<string>)obj), 
            o => o is IEnumerable<string>);

        public MainWindowViewModel(ApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
            Documents = new ObservableCollection<DocumentViewModel>();
            MarkedLinesViewModel = new MarkedLinesViewModel(Documents);
        }

        public DocumentViewModel? CurrentDocument
        {
            get => _currentDocument;
            set
            {
                if (_currentDocument == value) return;
                
                if (_currentDocument != null)
                    _currentDocument.IsCurrentDocument = false;
                
                _currentDocument = value;
                
                if (_currentDocument != null)
                    _currentDocument.IsCurrentDocument = true;
                
                InvokePropertyChanged();
            }
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = "log",
                Filter = "All Files|*.*|Log files(*.log)|*.log|Text files(*.txt)|*.txt",
                Multiselect = true
            };

            var dialogResult = dialog.ShowDialog();
            if (dialogResult.GetValueOrDefault())
            {
                foreach (var fileName in dialog.FileNames)
                {
                    Trace.TraceInformation($"Open document {fileName}.");
                    AddDocument(fileName);
                }
            }
            
            ShowScratchPad?.Invoke(this, new EventArgs());
        }
        
        private void OpenFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                AddDocument(file);
            }
        }

        private void AddDocument(string fileName)
        {
            var container = new DocumentContainer(fileName, _applicationSettings.ColorSettings);
            var viewModel = container.GetDocumentViewModel();
            Documents.Add(viewModel);
            Documents.CollectionChanged += (o, e) =>
            {
                if (Documents.Contains(viewModel))
                    return;
                container.Dispose();
            };
            CurrentDocument = viewModel;
        }

        public event EventHandler? ShowScratchPad;
    }
}
