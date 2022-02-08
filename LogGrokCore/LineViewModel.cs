using System;
using LogGrokCore.Controls;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public class LineViewModel : BaseLogLineViewModel
    {
        private readonly string _sourceString;

        private readonly ParseResult _parseResult;
        private readonly string _transformResult;

        public LineViewModel(int index, string sourceString, ILineParser parser, Selection markedLines,
            TransformationPerformer transformationPerformer)
            : base(index, markedLines)
        {
            _sourceString = sourceString;
            _transformResult = transformationPerformer.Transform(sourceString); 
            _parseResult = parser.Parse(
                _transformResult);
        }

        public LinePartViewModel this[int index] => GetValue(index);

        private LinePartViewModel GetValue(int index)
        {
            var uniqueId = HashCode.Combine(base.Index, index);
            var lineMeta = _parseResult.Get().ParsedLineComponents;
            var text = _transformResult.Substring(lineMeta.ComponentStart(index),
                lineMeta.ComponentLength(index));

            return new LinePartViewModel(uniqueId, text);
        }

        public override bool Equals(object? o)
        {
            if (o is LineViewModel other)
                return other.Index == Index && other._sourceString == _sourceString;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, _sourceString);
        }

        public override string ToString()
        {
            return _transformResult.TrimEnd();
        }
    }
}