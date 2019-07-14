using LogGrokCore.Data;

namespace LogGrokCore
{
    public class LineViewModel
    {
        private readonly string _sourceString;
        private readonly ParseResult _parseResult;
        
        public LineViewModel(int index, string sourceString, ILineParser parser)
        {
            Index = index;
            _sourceString = sourceString;
            _parseResult = parser.Parse(sourceString);
        }

        public int Index { get; }
    
        public string GetValue(int index)
        {
            var lineMeta = _parseResult.Get().ParsedLineComponents;
            
            return _sourceString.Substring(lineMeta.ComponentStart(index), lineMeta.ComponentLength(index)).TrimEnd();
        }
    }
}