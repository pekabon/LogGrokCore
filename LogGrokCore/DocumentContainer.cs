using System;
using System.Windows.Controls;
using DryIoc;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Virtualization;
using LogGrokCore.Filter;
using LogGrokCore.Search;

namespace LogGrokCore
{
    public enum ParserType
    {
        Full,
        OnlyIndexed
    }

    public enum GridViewType
    {
        FilteringGirdViewType,
        NotFilteringGridViewType
    }

    internal class DocumentContainer : IDisposable
    {
        private readonly Container _container;

        public DocumentContainer(string fileName)
        {
            var regex =
                @"^(?'Time'\d{2}\:\d{2}\:\d{2}\.\d{3})\t(?'Thread'0x[0-9a-fA-F]+)\t(?'Severity'\w+)\t(?'Component'[\w\.]+)?\t?(?'Message'.*)";
            _container = new Container(rules =>
                rules
                    .WithDefaultReuse(Reuse.Singleton)
                    .WithUnknownServiceResolvers
                        (Rules.AutoResolveConcreteTypeRule()));

            LoggerRegistrationHelper.Register(_container);
            
            _container.Register<Loader>();
            
            _container.Register<LineIndex>();
            _container.RegisterMapping<ILineIndex, LineIndex>();

            _container.Register<IItemProvider<(int, string)>, LineProvider>();

            _container.RegisterDelegate<ILineParser>(
                r => new RegexBasedLineParser(r.Resolve<LogMetaInformation>(), true), 
                serviceKey: ParserType.OnlyIndexed);

            _container.RegisterDelegate<ILineParser>(
                r => new RegexBasedLineParser(r.Resolve<LogMetaInformation>()), 
                serviceKey: ParserType.Full);
            
            _container.Register<ILineDataConsumer, LineProcessor>(
                made: Parameters.Of.Type<ILineParser>(serviceKey: ParserType.OnlyIndexed));
            
            _container.Register<ParsedBufferConsumer>(Reuse.Singleton);
            
            _container.Register<LogViewModel>(
                made: Parameters.Of
                    .Type<ILineParser>(serviceKey: ParserType.Full)
                    .Type<GridViewFactory>(serviceKey: GridViewType.FilteringGirdViewType));

            _container.Register<LogModelFacade>(
                made: Parameters.Of.Type<ILineParser>(serviceKey: ParserType.Full));
            
            _container.Register<StringPool>(Reuse.Singleton);
            
            _container.RegisterDelegate(r => new LogFile(fileName));
            _container.RegisterDelegate(r => new LogMetaInformation(regex, new[] {1, 2, 3}));
            
            _container.Register<Indexer>(Reuse.Singleton);
            _container.Register<LineViewModelCollectionProvider>(Reuse.Singleton, 
                made: Parameters.Of.Type<ILineParser>(serviceKey: ParserType.Full));

            _container.Register<DocumentViewModel>();
            _container.Register<SearchViewModel>();
            
            _container.RegisterDelegate<Func<string, FilterViewModel>>(
                _ => fieldName => new FilterViewModel(fieldName));

            _container.RegisterDelegate(
                r => new GridViewFactory(r.Resolve<LogMetaInformation>(),
                        true, r.Resolve<Func<string, FilterViewModel>>()), 
                serviceKey: GridViewType.FilteringGirdViewType);

            _container.RegisterDelegate(
                r => new GridViewFactory(r.Resolve<LogMetaInformation>(),
                    false, null), 
                serviceKey: GridViewType.NotFilteringGridViewType);

            _container.RegisterDelegate<Func<SearchPattern, SearchDocumentViewModel>>(
                r =>
            {
                var logFile = r.Resolve<LogFile>();
                var lineIndex = r.Resolve<LineIndex>();
                var lineParser = r.Resolve<ILineParser>(ParserType.Full);
                var viewFactory = r.Resolve<GridViewFactory>(GridViewType.NotFilteringGridViewType);
                return pattern => new SearchDocumentViewModel(logFile, lineIndex, lineParser, viewFactory, pattern);
            });
            
        }

        public DocumentViewModel GetDocumentViewModel() => _container.Resolve<DocumentViewModel>(); 

        public void Dispose() => _container.Dispose();
    }
}
