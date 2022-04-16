using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Monikers;
using LogGrokCore.Data.Search;

namespace LogGrokCore.Data;

internal readonly struct ParseTaskData : IDisposable
{
    public IMemoryOwner<byte> Memory { get; init; } 
        
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
    private readonly StringPool _stringPool;
    private readonly Encoding _encoding;
    private readonly ILineParser _parser;
    private readonly int _componentCount;
    private readonly LineIndex _lineIndex;
    private readonly Indexer _indexer;

    private readonly Channel<ParseTaskData> _parseTaskDataChannel;
    private readonly ChannelWriter<ParseTaskData> _parseTaskDataChannelWriter;

    private readonly Channel<ValueTask<(long bufferStartOffset, int lineCount, string parsedBuffer)>>
        _parseResultsChannel;

    private readonly ChannelWriter<ValueTask<(long bufferStartOffset, int lineCount, string parsedBuffer)>>
        _parseResultsChannelWriter;

    private readonly List<Task> _workers = new();
    private long _lineOffsetFromBufferStart;
    private readonly TaskCompletionSource _consumerCompletionSource = new();

    public LineProcessor(LogFile logFile,
        LogMetaInformation metaInformation,
        ILineParser parser,
        StringPool stringPool, LineIndex lineIndex, Indexer indexer)
    {
        _encoding = logFile.Encoding;
        _componentCount = metaInformation.IndexedFieldNumbers.Length;

        _stringPool = stringPool;
        _lineIndex = lineIndex;
        _indexer = indexer;
        _parser = parser;

        _parseTaskDataChannel = Channel.CreateBounded<ParseTaskData>(
            new BoundedChannelOptions(Environment.ProcessorCount)
            {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false,
                SingleWriter = false,
                SingleReader = false
            });

        _parseTaskDataChannelWriter = _parseTaskDataChannel.Writer;

        _parseResultsChannel =
            Channel.CreateBounded<ValueTask<(long bufferStartOffset, int lineCount, string parsedBuffer)>>(
                new BoundedChannelOptions(Environment.ProcessorCount)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    AllowSynchronousContinuations = true,
                    SingleWriter = false,
                    SingleReader = true
                });

        _parseResultsChannelWriter = _parseResultsChannel.Writer;

        _workers.Add(StartAsyncTask(() => ConsumeParseResults(_consumerCompletionSource)));

        var parsersCount = Math.Max(1, Environment.ProcessorCount - 1);
        for (var i = 0; i < parsersCount; i++)
        {
            _workers.Add(StartAsyncTask(ProcessParseTaskData));
        }
    }

    private Task StartAsyncTask(Func<Task> action,
        [CallerArgumentExpression("action")] string actionExpression = "")
    {
        return Task.Factory.StartNew(async () =>
        {
            try
            {
                await action();
            }
            catch (OperationCanceledException)
            {
                Trace.TraceInformation($"Task '{actionExpression}' cancelled.");
            }
        });
    }

    public async Task CompleteAdding(long totalBytesRead)
    {
        _parseResultsChannelWriter.Complete();
        _parseTaskDataChannelWriter.Complete();

        await _consumerCompletionSource.Task;

        _lineIndex.Finish((int)(totalBytesRead - _lineOffsetFromBufferStart));
        _indexer.Finish();
    }

    public async Task AddLineData(
        IMemoryOwner<byte> memory,
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

    private async Task ProcessParseTaskData()
    {
        var counter = 0;
        var lineCount = 0;
        Trace.TraceInformation("ProcessParseTaskData started");
        await foreach (var taskData in _parseTaskDataChannel.Reader.ReadAllAsync())
        {
            try
            {
                if (counter == 0)
                {
                    Trace.TraceInformation("Processing first chunk of data");
                }
                var parseResult = Parse(taskData);
                lineCount += parseResult.bufferLineCount;
                taskData.Completion.SetResult(parseResult);
                counter++;
            }
            finally
            {
                taskData.Dispose();
            }
        }

        Trace.TraceInformation($"Processed {counter} chunks of data, lineCount={lineCount}");

    }

    private (long bufferOffset, int bufferLineCount, string buffer) Parse(ParseTaskData taskData)
    {
        var lineCount = taskData.Lines.Count;

        var metaSizeChars =
            LineMetaInformation.GetSizeChars(_componentCount);
        var necessarySpaceChars = metaSizeChars * lineCount +
                                  _encoding.GetMaxCharCount(taskData.Memory.Memory.Length);
        var stringBuffer = _stringPool.Rent(necessarySpaceChars);
        var currentBufferPosition = 0;
        var bufferLineCount = 0;
        var sourceSpan = taskData.Memory.Memory.Span;
        var (bufferOffset, _, _) = taskData.Lines[0];

        foreach (var (lineOffset, start, length) in taskData.Lines)
        {
            unsafe
            {
                var lineData = sourceSpan.Slice(start, length);
                fixed (char* stringPointer = stringBuffer.AsSpan(currentBufferPosition))
                {
                    var decodedStringSpan =
                        new Span<char>(stringPointer + metaSizeChars,
                            stringBuffer.Length - currentBufferPosition);

                    var stringLength = _encoding.GetChars(lineData, decodedStringSpan);
                    var stringFrom = currentBufferPosition + metaSizeChars;

                    var lineMetaInformation =
                        LineMetaInformation.Get(stringPointer, _componentCount);

                    if (_parser.TryParse(stringBuffer, stringFrom, stringLength,
                            lineMetaInformation.ParsedLineComponents))
                    {
                        lineMetaInformation.LineOffsetFromBufferStart = (int)(lineOffset - bufferOffset);
                        currentBufferPosition += lineMetaInformation.TotalSizeWithPayloadCharsAligned;
                        bufferLineCount++;
                    }
                }
            }
        }

        return (bufferOffset, bufferLineCount, stringBuffer);
    }

    private async Task ConsumeParseResults(TaskCompletionSource taskCompletionSource)
    {
        await foreach (var parseResult in _parseResultsChannel.Reader.ReadAllAsync())
        {
            var (bufferStartOffset, lineCount, parsedBuffer) = await parseResult;
            AddParsedBuffer(bufferStartOffset, lineCount, parsedBuffer);
        }

        taskCompletionSource.SetResult();
        Trace.TraceInformation("ConsumeParseResults finished.");
    }

    public unsafe void AddParsedBuffer(long bufferStartOffset, int lineCount, string buffer)
    {
        var metaOffset = 0;
        fixed (char* start = buffer)
        {
            for (var idx = 0; idx < lineCount; idx++)
            {
                var lineMetaInformation = LineMetaInformation.Get(start + metaOffset, _componentCount);
                _lineOffsetFromBufferStart = bufferStartOffset +
                                             lineMetaInformation.LineOffsetFromBufferStart;
                var lineNum = _lineIndex.Add(_lineOffsetFromBufferStart);

                var indexKey = new IndexKey(buffer, metaOffset, _componentCount);
                _indexer.Add(indexKey, lineNum);
                metaOffset += lineMetaInformation.TotalSizeWithPayloadCharsAligned;
            }
        }

        _stringPool.Return(buffer);
    }
}