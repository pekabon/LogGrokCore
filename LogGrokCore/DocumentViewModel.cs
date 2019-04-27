using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
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
            OpenContainingFolderCommand = ReactiveCommand.Create(OpenContainingFolder);
        }

        public ICommand CopyPathToClipboardCommand { get; }
        public ICommand OpenContainingFolderCommand { get; }
        public string Title { get; }
        
        public string Text { get; }

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
