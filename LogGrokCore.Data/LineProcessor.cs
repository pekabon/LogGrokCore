using System;
using System.Text;
using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data
{
    public class LineProcessor : ILineDataConsumer
    {
        private const int InitialBufferSize = 64 * 1024;
        private readonly StringPool _stringPool;

        private string? _currentString;
        
        private int _currentOffset;
        private int _currentBufferLineCount;
        private long _bufferOffset;
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

        public void CompleteAdding()
        {
            if (_currentString != null)
            {
                _parsedBufferConsumer.AddParsedBuffer(_bufferOffset, _currentBufferLineCount, _currentString);
            }
            
            _parsedBufferConsumer.CompleteAdding();
        }

        public unsafe bool AddLineData(long lineOffset, Span<byte> lineData)
        {
            var metaSizeChars =
                LineMetaInformation.GetSizeChars(_componentCount); 

            var necessarySpaceChars = metaSizeChars + _encoding.GetMaxCharCount(lineData.Length);
            
            if (_currentString == null)
            {
                _currentString = SwitchToNewBuffer(necessarySpaceChars, lineOffset);
            }
            else if (_currentString.Length - _currentOffset < necessarySpaceChars)
            {
                _parsedBufferConsumer.AddParsedBuffer(_bufferOffset, _currentBufferLineCount, _currentString);
                _currentString = SwitchToNewBuffer(necessarySpaceChars, lineOffset);
            }

            fixed (char* stringPointer = _currentString.AsSpan(_currentOffset))
            {
                var decodedStringSpan =
                    new Span<char>(stringPointer + metaSizeChars, _currentString.Length - _currentOffset);
                var stringLength = _encoding.GetChars(lineData, decodedStringSpan);
                var stringFrom = _currentOffset + metaSizeChars;
                
                var lineMetaInformation =
                    LineMetaInformation.Get(stringPointer, _componentCount);

                
                if (_parser.TryParse(_currentString, stringFrom, stringLength,
                    lineMetaInformation.ParsedLineComponents))
                {
                    lineMetaInformation.LineOffsetFromBufferStart = (int)(lineOffset - _bufferOffset);
                    _currentOffset += lineMetaInformation.TotalSizeWithPayloadCharsAligned;
                    _currentBufferLineCount++;
                    return true;
                }

                if (_currentOffset == 0)
                {
                    _bufferOffset += lineData.Length;
                }
        
                return false;
            }

            string SwitchToNewBuffer(int minimumBufferSizeChars, long currentLineOffset)
            {
                _currentOffset = 0;
                _bufferOffset = currentLineOffset;
                _currentBufferLineCount = 0;
                return _stringPool.Rent((minimumBufferSizeChars / InitialBufferSize + 1) * InitialBufferSize);
            }
        }
    }
}