using LogGrokCore.Data;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore
{
    public class Document
    {
        private readonly Loader _loader;
        
        public string FilePath { get; }

        public VirtualList<string> Lines { get; }

        public bool IsLoading => _loader.IsLoading;

        public Document(LogMetaInformation metaInformation, Loader loader, LineProvider lineProvider)
        {
            FilePath = metaInformation.FileName;
            _loader = loader;
            Lines = new VirtualList<string>(lineProvider);
        }
    }
}
