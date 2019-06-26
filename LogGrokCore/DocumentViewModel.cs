using ReactiveUI;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    internal class DocumentViewModel : ReactiveObject
    {
        private readonly Document _document;
        private GridViewFactory _viewFactory;

        public DocumentViewModel(Document document, GridViewFactory viewFactory)
        {
            _document = document;
            Title = Path.GetFileName(document.FilePath);

            var lineCollection =
                new VirtualList<string, LineViewModel>(document.LineProvider, s => new LineViewModel(s));
            Lines = new GrowingCollectionAdapter<LineViewModel>(lineCollection);
                
            CopyPathToClipboardCommand = new DelegateCommand(() => TextCopy.Clipboard.SetText(_document.FilePath));
            OpenContainingFolderCommand = new DelegateCommand(OpenContainingFolder);

            _viewFactory = viewFactory;
            UpdateDocumentWhileLoading();
        }

        public ViewBase CustomView => _viewFactory.CreateView();

        public ICommand CopyPathToClipboardCommand { get; }
        public ICommand OpenContainingFolderCommand { get; }
        public string Title { get; }

        public GrowingCollectionAdapter<LineViewModel> Lines { get; }

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
