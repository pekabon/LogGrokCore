using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using DryIoc;
using DynamicData.Binding;
using LogGrokCore.Data;
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
                    AddDocument(fileName);
                }
            }
        }

        private void AddDocument(string fileName)
        {
            var regex =
                new Regex(
                    @"^(?'Time'\d{2}\:\d{2}\:\d{2}\.\d{3})\t(?'Thread'0x[0-9a-fA-F]+)\t(?'Severity'\w+)\t(?'Message'.*)",
                    RegexOptions.Compiled);

            var container = new Container(rules =>
                rules
                    .WithUnknownServiceResolvers(Rules.AutoResolveConcreteTypeRule())
                    .WithDefaultReuse(Reuse.Singleton));

            container.Register<Loader>();
            container.Register<ILineParser, TabBasedLineParser>();
            container.Register<ILineDataConsumer, LineProcessor>();
            container.RegisterDelegate(
                r => new LogMetaInformation(
                    fileName, r.Resolve<Func<Stream>>(), regex, 2));
            container.UseInstance(new Func<Stream>(() => OpenFile(fileName)));
            
            var viewModel = container.Resolve<DocumentViewModel>();
            Documents.Add(viewModel);
            
            Documents.CollectionChanged += (o, e) =>
            {
                if (Documents.Contains(viewModel))
                    return;
                container.Dispose();
            };
        }

        private static Stream OpenFile(string fileName)
        {
            const int bufferSize = 64 * 1024;
            return new FileStream(fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize,
                options: FileOptions.SequentialScan);
        }

    }
}
