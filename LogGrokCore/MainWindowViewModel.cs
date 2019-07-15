using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;

namespace LogGrokCore
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private DocumentViewModel? _currentDocument;

        public ObservableCollection<DocumentViewModel> Documents { get; } =
            new ObservableCollection<DocumentViewModel>();
        public ICommand OpenFileCommand => new DelegateCommand(OpenFile);

        public DocumentViewModel CurrentDocument
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
                    AddDocument(fileName);
                }
            }
        }

        private void AddDocument(string fileName)
        {
            var container = new DocumentContainer(fileName);
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
    }
}
