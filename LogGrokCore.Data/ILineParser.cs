using System;

namespace LogGrokCore.Data
{
    public struct ParseResult
    {
        private readonly int[] _metaPlaceHolder;
        private readonly int _componentCount;

        internal ParseResult(int componentCount, int[] metaPlaceHolder)
        {
            _metaPlaceHolder = metaPlaceHolder;
            _componentCount = componentCount;
        }

        public LineMetaInformation Get()
        {
            return new LineMetaInformation(_metaPlaceHolder.AsSpan(), _componentCount);
        }
    }

    public interface ILineParser
    {
        bool TryParse(string input, int beginning, int length, in LineMetaInformation lineMeta);

        ParseResult Parse(string input);
    }
}