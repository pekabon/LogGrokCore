using System;
using System.Text;

namespace LogGrokCore.Data
{
    public class LineProcessor : ILineDataConsumer
    {
     private const int InitialBufferSize = 512 * 1024;
        private readonly StringPool _stringPool;

        private string _currentString;
        private string _previousLineString;

        private int _currentOffset;
        private int _previousOffset = -1;
        private readonly Encoding _encoding;
        private readonly ILineParser _parser;
        private readonly int _componentCount;

        public LineProcessor(LogFile logFile, LogMetaInformation metaInformation, ILineParser parser)
        {
            _encoding = logFile.Encoding;
            _componentCount = metaInformation.ComponentCount;
            _stringPool = new StringPool();
            _currentString = _stringPool.Rent(InitialBufferSize);
            _previousLineString = _currentString;
            
            _parser = parser;
        }

        public unsafe bool AddLineData(Span<byte> lineData)
        {
            var metaSizeChars =
                LineMetaInformationNode
                    .GetSizeChars(_componentCount); // line size + two components (start & length for each)

            var necessarySpaceChars = metaSizeChars + _encoding.GetMaxCharCount(lineData.Length);

            if (_currentString.Length - _currentOffset < necessarySpaceChars)
            {
                // TODO send data for further processing
                _stringPool.Return(_currentString);
                _currentString = _stringPool.Rent((necessarySpaceChars / InitialBufferSize + 1) * InitialBufferSize);
                _currentOffset = 0;
            }

            bool TryGetPreviousNode(out LineMetaInformationNode node)
            {
                if (_previousOffset >= 0)
                {
                    fixed (char* previousMetadataPointer = _previousLineString.AsSpan(_previousOffset))
                    {
                        node = LineMetaInformationNode.Get(previousMetadataPointer, _componentCount);
                        return true;
                    }
                }

                node = default;
                return false;
            }

            fixed (char* stringPointer = _currentString.AsSpan(_currentOffset))
            {
                var decodedStringSpan =
                    new Span<char>(stringPointer + metaSizeChars, _currentString.Length - _currentOffset);
                var stringLength = _encoding.GetChars(lineData, decodedStringSpan);
                var stringFrom = _currentOffset + metaSizeChars;

                var node = LineMetaInformationNode.Get(stringPointer, _componentCount);
                var lineMetaInformation = node.LineMetaInformation;

                if (!_parser.TryParse(_currentString, stringFrom, stringLength, lineMetaInformation))
                {
                    if (TryGetPreviousNode(out var prevNode))
                    {
                        prevNode.LineMetaInformation.LineLength += stringLength;
                    }
                    
                    return false;
                }

                _previousOffset = _currentOffset;
                _currentOffset += node.TotalSizeCharsAligned;

                if (_previousLineString == _currentString && TryGetPreviousNode(out var previousNode))
                {
                    previousNode.NextNodeOffset = _currentOffset;
                }
                else
                {
                    _previousOffset = -1;
                    _previousLineString = _currentString;

                    //FinishLineSet(necessarySpaceChars);
                    // TODO send previous line to processing;
                }
            }

            return true;
        }

        private void FinishLineSet(int necessarySpace)
        {
            _stringPool.Return(_currentString);
//            var lineSet = new LineSet(_currentString, _componentCount);
            _currentString =
                necessarySpace <= InitialBufferSize
                    ? _stringPool.Rent(InitialBufferSize)
                    : _stringPool.Rent((necessarySpace / InitialBufferSize + 1) * InitialBufferSize);
            _currentOffset = 0;
        }
    }
}