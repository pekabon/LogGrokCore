using LogGrokCore.Controls;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public class LineViewModel : ItemViewModel
    {
        private readonly string _sourceString;
        private readonly Selection _markedLines;
        private readonly ParseResult _parseResult;

        public LineViewModel(int index, string sourceString, ILineParser parser, Selection markedLines)
        {
            Index = index;
            _sourceString = sourceString;
            _markedLines = markedLines;
            _parseResult = parser.Parse(sourceString);
            _markedLines.Changed += () => InvokePropertyChanged(nameof(IsMarked));
        }

        public bool IsMarked
        {
            get => _markedLines.Contains(Index);
            set
            {
                if (value)
                    _markedLines.Add(Index);
                else 
                    _markedLines.Remove(Index);
            }
        }

        public int Index { get; }

        public string this[int index] => GetValue(index);

        public string GetValue(int index)
        {
            var lineMeta = _parseResult.Get().ParsedLineComponents;
            return _sourceString.Substring(lineMeta.ComponentStart(index),
                    lineMeta.ComponentLength(index))
                .TrimEnd('\0').TrimEnd();
        }

        public override string ToString()
        {
            return _sourceString;
        }
    }
}