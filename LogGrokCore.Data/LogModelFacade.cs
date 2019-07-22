using LogGrokCore.Data.Index;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data
{
    public class LogModelFacade
    {
        private readonly Loader _loader;
        private readonly long _fileSize;

        public LogModelFacade(
            LogFile logFile,
            Loader loader,
            LineIndex lineIndex,
            LineProvider lineProvider,
            ILineParser lineParser,
            Indexer indexer,
            LogMetaInformation metaInformation)
        {
            FilePath = logFile.FilePath;
            _fileSize = logFile.FileSize;
            LineIndex = lineIndex;
            _loader = loader;
            LineProvider = lineProvider;
            LineParser = lineParser;
            Indexer = indexer;
            MetaInformation = metaInformation;
        }

        public LogMetaInformation MetaInformation { get; }

        public Indexer Indexer { get; }

        public ILineParser LineParser { get; }
        
        public IItemProvider<string> LineProvider { get; }
        
        public string FilePath { get; }
        
        private LineIndex LineIndex { get; }

        public double LoadProgress
        {
            get
            {
                if (LineIndex.Count == 0)
                    return 0;
                if (LineIndex.IsFinished)
                    return 100;
                
                var (lastLineOffset, _) = LineIndex.GetLine(LineIndex.Count - 1);
                return (double) lastLineOffset / _fileSize * 100.0;
            }
        }

        public bool IsLoaded => LineIndex.IsFinished;
    }
}