using System;

namespace LogGrokCore.Data
{
    public readonly struct ParseResult
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
}