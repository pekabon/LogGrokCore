using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using LogGrokCore.Data.Monikers;
using LogGrokCore.Data.Search;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace LogGrokCore.Data
{

    
    internal readonly struct ParseTaskData : IDisposable
    {
        public MemoryOwner<byte> Memory { get; init; } 
        
        public PooledList<(long offset, int start, int length)> Lines { get; init; }

        public ValueTaskSource<(long bufferStartOffset, int lineCount, string parsedBuffer)> Completion { get; init; }
            
        public void Dispose()
        {
            Memory.Dispose();
            Lines.Dispose();
        }
    }

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
        private readonly Channel<ParseTaskData> _parseTaskDataChannel;
        private readonly ChannelWriter<ParseTaskData> _parseTaskDataChannelWriter;

        private readonly Channel<ValueTask<(long bufferStartOffset, int lineCount, string parsedBuffer)>>
            _parseResultsChannel;
        private readonly ChannelWriter<ValueTask<(long bufferStartOffset, int lineCount, string parsedBuffer)>>
            _parseResultsChannelWriter;
        
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

            _parseTaskDataChannel = Channel.CreateBounded<ParseTaskData>(
                new BoundedChannelOptions(Environment.ProcessorCount)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    AllowSynchronousContinuations = true,
                    SingleWriter = true,
                    SingleReader = true
                });
            
            _parseTaskDataChannelWriter = _parseTaskDataChannel.Writer;
            
            _parseResultsChannel = Channel.CreateBounded<ValueTask<(long bufferStartOffset, int lineCount, string parsedBuffer)>>(
                new BoundedChannelOptions(Environment.ProcessorCount)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    AllowSynchronousContinuations = true,
                    SingleWriter = true,
                    SingleReader = true
                });

            _parseResultsChannelWriter = _parseResultsChannel.Writer;
            
            
        }

        async Task StartAsyncTask(Func<Task> action,
            [CallerArgumentExpression("action")] string actionExpression = "")
        {
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    await action();
                }
                catch (OperationCanceledException)
                {
                }
            } );
        }

        public void CompleteAdding(long totalBytesRead)
        {
            if (_currentString != null)
            {
                _parsedBufferConsumer.AddParsedBuffer(_bufferOffset, _currentBufferLineCount, _currentString);
            }
            
            _parsedBufferConsumer.CompleteAdding(totalBytesRead);
        }

        public async void AddLineData(
            MemoryOwner<byte> memory, 
            PooledList<(long offset, int start, int length)> lines)
        {

            ValueTaskSource<(long bufferStartOffset, int lineCount, string parsedBuffer)> parseTaskCompletionSource 
                = new();
            
            await _parseTaskDataChannelWriter.WriteAsync(
                new ParseTaskData
                {
                    Memory = memory,
                    Lines = lines,
                    Completion = parseTaskCompletionSource 
                });

            await _parseResultsChannel.Writer.WriteAsync(parseTaskCompletionSource.GetTask());
        }

        private async void ProcessParseTaskData()
        {
            await foreach (var p in _parseTaskDataChannel.Reader.ReadAllAsync())
            {
                
            }
        }

        public unsafe void AddLineData(long lineOffset, Span<byte> lineData)
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
                    return;
                }

                if (_currentOffset == 0)
                {
                    _bufferOffset += lineData.Length;
                }
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