using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using ReactiveUI;
namespace LogGrokCore
{
    class MainWindowViewModel
    {
        public ObservableCollection<DocumentViewModel> Documents { get; } =
            new ObservableCollection<DocumentViewModel>();
        public ICommand OpenFileCommand => ReactiveCommand.Create(OpenFile);

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
