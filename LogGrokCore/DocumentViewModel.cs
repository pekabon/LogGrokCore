using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LogGrokCore
{
    internal class DocumentViewModel : ReactiveObject
    {
        private readonly Document _document;
        public DocumentViewModel(Document document)
        {
            _document = document;
            Title = Path.GetFileName(document.FilePath);

            Lines = new GrowingCollectionAdapter<string>(_document.Lines);
                
            CopyPathToClipboardCommand = ReactiveCommand.Create(() => TextCopy.Clipboard.SetText(_document.FilePath));
            OpenContainingFolderCommand = ReactiveCommand.Create(OpenContainingFolder);

            UpdateDocumentWhileLoading();
        }

        public ICommand CopyPathToClipboardCommand { get; }
        public ICommand OpenContainingFolderCommand { get; }
        public string Title { get; }

        public GrowingCollectionAdapter<string> Lines { get; }

        private async void UpdateDocumentWhileLoading()
        {
            var delay = 10;
            while (_document.IsLoading)
            {
                await Task.Delay(delay);
                if (delay < 1000)
                    delay *= 2;
                Lines.UpdateCount();
            }
            Lines.UpdateCount();
        }
        private void OpenContainingFolder()
        {
            var filePath = _document.FilePath;

            var cmdLine = File.Exists(filePath)
                ? $"/select, {filePath}"
                : $"/select, {Directory.GetParent(filePath).FullName}";

            _ = Process.Start("explorer.exe", cmdLine);
        }
    }
}
