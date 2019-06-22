using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LineProcessor : ILineDataConsumer
    {
        private readonly Regex _regex = 
            new Regex(@"^(?'Time'\d{2}\:\d{2}\:\d{2}\.\d{3})\t(?'Thread'0x[0-9a-fA-F]+)\t(?'Severity'\w+)\t(?'Message'.*)", RegexOptions.Compiled);

        private const int InitialBufferSize = 512 * 1024;
        private StringPool _stringPool;

        private string _currentString;
        private string _previousLineString;

        private int _currentOffset;
        private int _previousOffset = -1;
        private readonly Encoding _encoding;
        private readonly TablBasedLineParser _parser;
        private const int ComponentCount = 2;
        
        public LineProcessor(Encoding encoding)
        {
            _encoding = encoding;
            _stringPool = new StringPool();
            _currentString = _stringPool.Rent(InitialBufferSize);
            _previousLineString = _currentString;

            _parser =  new TablBasedLineParser(_regex, ComponentCount);
        }

        public unsafe bool AddLineData(Span<byte> lineData)
        {
            var metaSizeChars = LineMetaInformationNode.GetSizeChars(ComponentCount); // line size + two components (start & length for each)

            _encoding.GetMaxCharCount(lineData.Length);
            var necessarySpaceChars = metaSizeChars + _encoding.GetMaxCharCount(lineData.Length) ;

            if (_currentString.Length - _currentOffset < necessarySpaceChars)
            {
                _currentString = _stringPool.Rent((necessarySpaceChars / InitialBufferSize + 1) * InitialBufferSize);
                _currentOffset = 0;
            }
            
            bool TryGetPreviousNode(out LineMetaInformationNode node)
            {
                if (_previousOffset >= 0)
                {
                    fixed (char* previousMetadataPointer = _previousLineString.AsSpan(_previousOffset))
                    {
                        node = LineMetaInformationNode.Get(previousMetadataPointer, ComponentCount);
                        return true;
                    }
                }
                node = default;
                return false;
            }

            fixed (char* stringPointer = _currentString.AsSpan(_currentOffset))
            {
                var decodedStringSpan = new Span<char>(stringPointer + metaSizeChars, _currentString.Length - _currentOffset);
                var stringLength = _encoding.GetChars(lineData, decodedStringSpan);
                var stringFrom = _currentOffset + metaSizeChars;

                var node = LineMetaInformationNode.Get(stringPointer, ComponentCount);
                var lineMetaInformation = node.LineMetaInformation;

                if (!_parser.Parse(_currentString, stringFrom, stringLength, lineMetaInformation))
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
                    
                    // TODO send previous line to processing;
                }
            }

            return true;
        }

        private void FinishLineSet(int necessarySpace)
        {
            var lineSet = new LineSet(_currentString, ComponentCount);
            _currentString = 
                necessarySpace <= InitialBufferSize
                    ? _stringPool.Rent(InitialBufferSize)
                    : _stringPool.Rent((necessarySpace / InitialBufferSize + 1) * InitialBufferSize);
            _currentOffset = 0;
        }
    }
}