using System;
using System.Diagnostics;
using System.Text;
using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data
{
    public class LineProcessor : ILineDataConsumer
    {
     private const int InitialBufferSize = 512 * 1024;
        private readonly StringPool _stringPool;

        private string? _currentString;
        private string? _previousLineString;
        private long _bufferOffset;
        private int _currentOffset;
        private int _previousOffset = -1;
        private readonly Encoding _encoding;
        private readonly ILineParser _parser;
        private readonly int _componentCount;
        private readonly ParsedBufferConsumer _parsedBufferConsumer;

        public LineProcessor(LogFile logFile, 
            LogMetaInformation metaInformation, 
            ILineParser parser, 
            ParsedBufferConsumer parsedBufferConsumer,
            StringPool stringPool)
        {
            _encoding = logFile.Encoding;
            _componentCount = metaInformation.IndexedFieldNumbers.Length;
            _parsedBufferConsumer = parsedBufferConsumer;
            _stringPool = stringPool;
            
            _parser = parser;
        }

        public unsafe bool AddLineData(long lineOffset, Span<byte> lineData)
        {
            var metaSizeChars =
                LineMetaInformationNode
                    .GetSizeChars(_componentCount); // line size + two components (start & length for each)

            var necessarySpaceChars = metaSizeChars + _encoding.GetMaxCharCount(lineData.Length);
            
            if (_currentString == null)
            {
                _currentString = 
                    _stringPool.Rent((necessarySpaceChars / InitialBufferSize + 1) * InitialBufferSize);
                _previousLineString = _currentString;
                _bufferOffset = lineOffset;
            } 
            else if (_currentString.Length - _currentOffset < necessarySpaceChars)
            {
                // send data for further processing
                _parsedBufferConsumer.AddParsedBuffer(_currentString);
                _currentString = 
                    _stringPool.Rent((necessarySpaceChars / InitialBufferSize + 1) * InitialBufferSize);
                _currentOffset = 0;
                _bufferOffset = lineOffset;
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
                
                if (!_parser.TryParse(_currentString, stringFrom, stringLength, lineMetaInformation.ParsedLineComponents))
                {
                    if (TryGetPreviousNode(out var prevNode))
                    {
                        prevNode.LineMetaInformation.LineLength += stringLength;
                    }
                    else
                    {
                        _bufferOffset = lineOffset;
                    }
                    return false;
                }

                _previousOffset = _currentOffset;
                _currentOffset += node.TotalSizeCharsAligned;

                Debug.Assert(_currentOffset <= _currentString.Length);

                if (_previousLineString == _currentString && TryGetPreviousNode(out var previousNode))
                {
                    previousNode.NextNodeOffset = _currentOffset;
                }
                else
                {
                    _previousOffset = -1;
                    _previousLineString = _currentString;
                }
            }

            return true;
        }
    }
}