using LogGrokCore.Data.Index;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data
{
    public class LogModelFacade
    {
        public LogModelFacade(
            LogFile logFile,
            LineIndex lineIndex,
            LineProvider lineProvider,
            ILineParser lineParser,
            Indexer indexer,
            LogMetaInformation metaInformation)
        {
            LogFile = logFile;
            LineIndex = lineIndex;
            LineProvider = lineProvider;
            LineParser = lineParser;
            Indexer = indexer;
            MetaInformation = metaInformation;
        }

        public LogMetaInformation MetaInformation { get; }

        public Indexer Indexer { get; }

        public ILineParser LineParser { get; }
        
        public IItemProvider<(int, string)> LineProvider { get; }
        
        internal LineIndex LineIndex { get; }

        public double LoadProgress
        {
            get
            {
                if (LineIndex.Count == 0)
                    return 0;
                if (LineIndex.IsFinished)
                    return 100;
                
                var (lastLineOffset, _) = LineIndex.GetLine(LineIndex.Count - 1);
                return (double) lastLineOffset / LogFile.FileSize * 100.0;
            }
        }
        
        public LogFile LogFile { get; }

        public bool IsLoaded => LineIndex.IsFinished;
    }
}