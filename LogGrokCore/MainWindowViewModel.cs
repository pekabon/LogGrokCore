using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LogGrokCore.AvalonDock;
using LogGrokCore.MarkedLines;
using Microsoft.Win32;

namespace LogGrokCore
{
    public class MainWindowViewModel : ViewModelBase, IContentProvider
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
            OpenSettings = new DelegateCommand(() =>
            { 
                OpenExternalFile(ApplicationSettings.SettingsFileName);
            });

            MarkedLinesViewModel.NavigationRequested += (document, index) =>
            {
                CurrentDocument = document;
                document.NavigateTo(index);
            };
        }

        private static void OpenExternalFile(string fileName)
        {
            void StartProcess(string verb)
            {
                using var process = new Process
                {
                    StartInfo =
                    {
                        FileName = fileName,
                        UseShellExecute = true,
                        Verb = verb
                    }
                };
                process.Start();
            }

            try
            {
                StartProcess(string.Empty);
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == 1155) // 'No application is associated with the specified file for this operation.'
                {
                    StartProcess("openas");
                }
                else throw;
            }
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

        public ICommand OpenSettings { get; }

        public ICommand ExitCommand => new DelegateCommand(() => Application.Current.Shutdown());

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

        public void AddDocument(string fileName)
        {
            if (File.Exists(fileName))
                CurrentDocument = CreateDocument(fileName);
            else
                Trace.TraceError($"File {fileName} is not exists");
        }

        private DocumentViewModel CreateDocument(string fileName)
        {
            var container = new DocumentContainer(fileName, _applicationSettings);
            var viewModel = container.GetDocumentViewModel();
            Documents.Add(viewModel);
            Documents.CollectionChanged += (o, e) =>
            {
                if (Documents.Contains(viewModel))
                    return;
                container.Dispose();
            };
            return viewModel;
        }

        public event EventHandler? ShowScratchPad;
        
        public object? GetContent(string contentId)
        {
            return contentId == Constants.MarkedLinesContentId ? MarkedLinesViewModel : null;
        }
    }
}
