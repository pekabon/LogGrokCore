using System.Windows.Shapes;
using LogGrokCore.Data;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    public class Document
    {
        private readonly Loader _loader;
        
        public string FilePath { get; }

        public IItemProvider<string> LineProvider { get; }

        public bool IsLoading => _loader.IsLoading;

        public Document(LogMetaInformation metaInformation, Loader loader, IItemProvider<string> lineProvider)
        {
            FilePath = metaInformation.FileName;
            _loader = loader;
            LineProvider = lineProvider;
        }
    }
    }
