using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using ReactiveUI;
namespace LogGrokCore
{
    internal class MainWindowViewModel
    {
        public ObservableCollection<DocumentViewModel> Documents { get; } =
            new ObservableCollection<DocumentViewModel>();
        public ICommand OpenFileCommand => new DelegateCommand(OpenFile);

        public string? CurrentDocument { get; set; }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog()
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
                    Documents.Add(new DocumentViewModel
                    (
                        new Document(fileName)
                    ));
                }
            }
        }
    }
}
