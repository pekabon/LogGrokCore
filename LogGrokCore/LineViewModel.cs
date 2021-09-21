using LogGrokCore.Controls;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public class LineViewModel : BaseLogLineViewModel
    {
        private readonly string _sourceString;

        private readonly ParseResult _parseResult;

        public LineViewModel(int index, string sourceString, ILineParser parser, Selection markedLines)
            : base(index, markedLines)
        {
            _sourceString = sourceString;
            _parseResult = parser.Parse(sourceString);
        }

        public LinePartViewModel this[int index] => GetValue(index);

        public LinePartViewModel GetValue(int index)
        {
            var lineMeta = _parseResult.Get().ParsedLineComponents;
            var text = _sourceString.Substring(lineMeta.ComponentStart(index),
                lineMeta.ComponentLength(index));

            return new LinePartViewModel(text);
        }

        public override string ToString()
        {
            return _sourceString.TrimEnd();
        }
    }
}