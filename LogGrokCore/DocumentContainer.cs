using System;
using System.Diagnostics;
using System.Linq;
using DryIoc;
using LogGrokCore.Controls;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Controls.ListControls;
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

        private readonly Container _container;
        public DocumentContainer(string fileName, ApplicationSettings applicationSettings, SearchAutocompleteCache autocompleteCache)
        {
            _container = new Container(rules =>
                rules
                    .WithDefaultReuse(Reuse.Singleton)
                    .WithUnknownServiceResolvers
                        (Rules.AutoResolveConcreteTypeRule()));

            LoggerRegistrationHelper.Register(_container);
            
            _container.RegisterInstance(autocompleteCache);
            _container.Register<StringPool>(Reuse.Singleton);
            _container.Register<LogModelFacade>(
                made: Parameters.Of.Type<ILineParser>(serviceKey: ParserType.Full));
            
            // loader pipeline
            _container.Register<Loader>(Reuse.Singleton);
            
            _container.RegisterDelegate<Func<ILineDataConsumer>>(c =>
                () =>
                {
                    var parser = c.Resolve<ILineParser>(serviceKey: ParserType.OnlyIndexed);
                    return c.Resolve<LineProcessor>(parser);
                });
            _container.Register<ILineDataConsumer, LineProcessor>(
                made: Parameters.Of.Type<ILineParser>(serviceKey: ParserType.OnlyIndexed));
            
            // log model
            _container.RegisterDelegate(c =>
            {
                var meta = c.Resolve<LogMetaInformation>();
                return new LogFile(fileName, meta.XorMask);
            });
            _container.RegisterDelegate(_ => LogMetaInformationProvider.GetLogMetaInformation(fileName,
                applicationSettings.LogFormats.Where(l => l.IsCorrect())),
                Reuse.Singleton);
            _container.Register<LineIndex>();
            _container.RegisterMapping<ILineIndex, LineIndex>();
            _container.Register<Indexer>(Reuse.Singleton);
            _container.Register<IItemProvider<(int, string)>, LineProvider>();

            _container.RegisterDelegate<ILineParser>(
                r => new RegexBasedLineParser(r.Resolve<LogMetaInformation>(), true), 
                serviceKey: ParserType.OnlyIndexed);
            
            _container.RegisterDelegate<Func<ILineParser>>(r => 
                () => r.Resolve<ILineParser>(ParserType.OnlyIndexed));

            _container.RegisterDelegate<ILineParser>(
                r => new RegexBasedLineParser(r.Resolve<LogMetaInformation>()), 
                serviceKey: ParserType.Full);
            
            // Presentation
            _container.RegisterInstance(applicationSettings.ColorSettings);
            _container.RegisterDelegate(c => 
                applicationSettings.GetColumnSettings(c.Resolve<LogMetaInformation>().LineRegex), 
                reuse: Reuse.Singleton);

            _container.Register<LogViewModel>(
                made: Parameters.Of
                    .Type<ILineParser>(serviceKey: ParserType.Full)
                    .Type<GridViewFactory>(serviceKey: GridViewType.FilteringGirdViewType));

            _container.Register<LineViewModelCollectionProvider>(Reuse.Singleton, 
                made: Parameters.Of
                    .Type<ILineParser>(serviceKey: ParserType.Full)
                    .Type(request => request.Container.Resolve<LogHeaderCollection>()));
            
            _container.Register<FilterSettings>(Reuse.Singleton);
            
            _container.Register<DocumentViewModel>();
            _container.Register<SearchViewModel>();
            _container.Register<Selection>(Reuse.Singleton);
            _container.RegisterDelegate(c => 
                new TransformationPerformer(c.Resolve<LogMetaInformation>().Transformations));
            
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
                    var markedLines = r.Resolve<Selection>();
                    var columnSettings = r.Resolve<ColumnSettings>();
                    var transformationPerformer = r.Resolve<TransformationPerformer>();
                    return pattern => new SearchDocumentViewModel(logModelFacade, filterSettings, viewFactory, pattern,
                        markedLines, columnSettings, transformationPerformer);
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
