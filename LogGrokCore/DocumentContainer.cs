using System;
using System.Collections.Generic;
using System.Diagnostics;
using DryIoc;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Virtualization;
using LogGrokCore.Filter;
using LogGrokCore.Search;

namespace LogGrokCore
{
    internal class DocumentContainer : IDisposable
    {
        private enum ParserType
        {
            Full,
            OnlyIndexed
        }

        private enum GridViewType
        {
            FilteringGirdViewType,
            NotFilteringGridViewType
        }

        private enum LineViewModelCollectionType
        {
            NotHeadered
        }
        
        private readonly Container _container;
        public DocumentContainer(string fileName)
        {
            _container = new Container(rules =>
                rules
                    .WithDefaultReuse(Reuse.Singleton)
                    .WithUnknownServiceResolvers
                        (Rules.AutoResolveConcreteTypeRule()));

            LoggerRegistrationHelper.Register(_container);
            _container.Register<StringPool>(Reuse.Singleton);
            _container.Register<LogModelFacade>(
                made: Parameters.Of.Type<ILineParser>(serviceKey: ParserType.Full));
            
            // loader pipeline
            _container.Register<Loader>(Reuse.Singleton);
            _container.Register<ILineDataConsumer, LineProcessor>(
                made: Parameters.Of.Type<ILineParser>(serviceKey: ParserType.OnlyIndexed));
            _container.Register<ParsedBufferConsumer>(Reuse.Singleton);
            
            // log model
            _container.RegisterDelegate(_ => new LogFile(fileName));
            _container.RegisterDelegate(_ => LogMetaInformationProvider.GetLogMetaInformation(fileName),
                Reuse.Singleton);
            _container.Register<LineIndex>();
            _container.RegisterMapping<ILineIndex, LineIndex>();
            _container.Register<Indexer>(Reuse.Singleton);
            _container.Register<IItemProvider<(int, string)>, LineProvider>();

            _container.RegisterDelegate<ILineParser>(
                r => new RegexBasedLineParser(r.Resolve<LogMetaInformation>(), true), 
                serviceKey: ParserType.OnlyIndexed);

            _container.RegisterDelegate<ILineParser>(
                r => new RegexBasedLineParser(r.Resolve<LogMetaInformation>()), 
                serviceKey: ParserType.Full);
            
            // Presentation

            _container.Register<LogViewModel>(
                made: Parameters.Of
                    .Type<ILineParser>(serviceKey: ParserType.Full)
                    .Type<GridViewFactory>(serviceKey: GridViewType.FilteringGirdViewType));

            _container.Register<LineViewModelCollectionProvider>(Reuse.Singleton, 
                made: Parameters.Of
                    .Type<ILineParser>(serviceKey: ParserType.Full)
                    .Type<Func<string?>>(request =>
                    {
                        var headerProvider = request.Container.Resolve<HeaderProvider>();
                        return () => headerProvider.Header;
                    }));
            
            _container.Register<LineViewModelCollectionProvider>(Reuse.Singleton, 
                made: Parameters.Of
                    .Type<ILineParser>(serviceKey: ParserType.Full)
                    .Type<Func<string?>>(request => () =>  null), serviceKey: LineViewModelCollectionType.NotHeadered);

            _container.Register<FilterSettings>(Reuse.Singleton);
            
            _container.Register<DocumentViewModel>();
            _container.Register<SearchViewModel>();
            
            _container.RegisterDelegate<Func<string, FilterViewModel>>(
                r => fieldName => new FilterViewModel(
                    fieldName, 
                    r.Resolve<FilterSettings>(),
                    r.Resolve<Indexer>(),
                    r.Resolve<LogMetaInformation>()));

            _container.RegisterDelegate<Func<SearchPattern, SearchDocumentViewModel>>(
                r =>
                {
                    var logModelFacade = r.Resolve<LogModelFacade>();
                    var filterSettings = r.Resolve<FilterSettings>();
                    var viewFactory = r.Resolve<GridViewFactory>(GridViewType.NotFilteringGridViewType);
                    return pattern => new SearchDocumentViewModel(logModelFacade, filterSettings, viewFactory, pattern);
                });
            
            // view
            _container.RegisterDelegate(
                r => new GridViewFactory(r.Resolve<LogMetaInformation>(),
                    true, r.Resolve<Func<string, FilterViewModel>>()), 
                serviceKey: GridViewType.FilteringGirdViewType);

            _container.RegisterDelegate(
                r => new GridViewFactory(r.Resolve<LogMetaInformation>(),
                    false, null), 
                serviceKey: GridViewType.NotFilteringGridViewType);


            // start loading
            _ = _container.Resolve<Loader>();
            Trace.TraceInformation($"Start loading {fileName}");
        }

        public DocumentViewModel GetDocumentViewModel() => _container.Resolve<DocumentViewModel>(); 

        public void Dispose() => _container.Dispose();
    }
}
