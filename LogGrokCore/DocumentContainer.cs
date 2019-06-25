using DryIoc;
using LogGrokCore.Data;
using System;
using System.Text.RegularExpressions;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    internal class DocumentContainer : IDisposable
    {
        private readonly Container _container;

        public DocumentContainer(string fileName)
        {
            var regex =
                new Regex(
                    @"^(?'Time'\d{2}\:\d{2}\:\d{2}\.\d{3})\t(?'Thread'0x[0-9a-fA-F]+)\t(?'Severity'\w+)\t(?'Message'.*)",
                    RegexOptions.Compiled);

            _container = new Container(rules =>
                rules
                    .WithDefaultReuse(Reuse.Singleton)
                    .WithUnknownServiceResolvers
                        (Rules.AutoResolveConcreteTypeRule()));

            _container.Register<Loader>();
            _container.Register<Document>();
            _container.Register<DocumentViewModel>();
            _container.Register<LineIndex>();
            _container.Register<IItemProvider<string>, LineProvider>();
            _container.Register<ILineParser, TabBasedLineParser>();
            _container.Register<ILineDataConsumer, LineProcessor>();
            _container.RegisterDelegate(
                r => new LogMetaInformation(
                    fileName, regex, 2));
        }

        public DocumentViewModel GetDocumentViewModel() => _container.Resolve<DocumentViewModel>(); 

        public void Dispose() => _container.Dispose();
    }
}
