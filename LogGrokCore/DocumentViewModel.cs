using ReactiveUI;
using System.IO;
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
            Text = document.Content;
            CopyPathToClipboardCommand = ReactiveCommand.Create(() => TextCopy.Clipboard.SetText(_document.FilePath));
        }

        public ICommand CopyPathToClipboardCommand { get; }

        public string Title { get; }

        public string Text { get; }
    }
}
