using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using ReactiveUI;
namespace LogGrokCore
{
    class DocumentViewModel : ReactiveObject
    {
        private string _title;
        private string _text;

        public DocumentViewModel(string title, string text)
        {
            _title = title;
            _text = text;
        }

        public string Title { get => _title; set => this.RaiseAndSetIfChanged(ref _title, value); }

        public string Text { get => _text; set => this.RaiseAndSetIfChanged(ref _text, value); }
    }

    class MainWindowViewModel
    {
        public ObservableCollection<DocumentViewModel> Documents => 
            new ObservableCollection<DocumentViewModel> {
                new DocumentViewModel("AAA", "AAAAA"), new DocumentViewModel("BBB", "BBBB") };
        public string? CurrentDocument { get; set; }
    }
}
