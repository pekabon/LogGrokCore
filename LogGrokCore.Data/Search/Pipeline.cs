using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Data.Search;

public class Pipeline
{
    private readonly Regex _regex;
    private readonly LogModelFacade _logModelFacade;
    private const int MaxSearchSizeLines = 1024;
    private readonly int _searchWorkersCount = Math.Max(Environment.ProcessorCount - 1, 1);
    private readonly StringPool _stringPool = new();

    private uint? _id;
    
    public Pipeline(Regex regex,
        LogModelFacade logModelFacade)
    {
        _regex = regex;
        _logModelFacade = logModelFacade;
    }

    private void Trace(string message)
    {
        unchecked
        {
            _id ??= (uint)GetHashCode(); 
        }

        System.Diagnostics.Trace.TraceInformation($"Search({_regex}, {_id}): {message}");
    }

    private async Task StartAsyncTask(Func<Task> action, CancellationToken cancellationToken,
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
                Trace($"Operation canceled: {actionExpression}.");
            }
        } , cancellationToken);
    }
    
    public async Task StartSearch(
        SubIndexer searchIndexer,
        SearchLineIndex lineIndex,
        Search.Progress progress,
        CancellationToken cancellationToken)
    {
        var timestamp = DateTime.Now;

        Trace($"Search started.");
        
        var encoding = _logModelFacade.LogFile.Encoding;
        var sourceLineIndex = _logModelFacade.LineIndex;
        var sourceIndexer = _logModelFacade.Indexer;

        var searchResultsChannel = Channel.CreateBounded<ValueTask<PooledList<int>>>(
            new BoundedChannelOptions(Environment.ProcessorCount)
            {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleWriter = true,
                SingleReader = true
            });

        var searchTasksChannel = Channel.CreateBounded<(IMemoryOwner<byte> memory, int startLine, int EndLine, 
            ValueTaskSource<PooledList<int>>)>(
            new BoundedChannelOptions(Environment.ProcessorCount)
            {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleWriter = true,
                SingleReader = false
            });


        var processSearchResultsCompletionSource = new TaskCompletionSource();
        var workers = new List<Task>
        {
            StartAsyncTask(() => LoadBuffersWorker(_logModelFacade, progress, 
                    searchResultsChannel.Writer, searchTasksChannel.Writer, cancellationToken),
                cancellationToken),
            StartAsyncTask(() => ProcessSearchResultsWorker(lineIndex, 
                 sourceIndexer, searchIndexer, searchResultsChannel.Reader, 
                 processSearchResultsCompletionSource, cancellationToken), cancellationToken)
        };

        await searchTasksChannel.StartConsumers(async reader =>
            await SearchInBufferWorker(_regex, sourceLineIndex, encoding, reader, cancellationToken),
            Environment.ProcessorCount);
        
        await Task.WhenAll(workers.ToArray());
        await processSearchResultsCompletionSource.Task;
        Trace($"Search finished, spent: {DateTime.Now-timestamp}");
    }

    private async Task LoadBuffersWorker(LogModelFacade logModelFacade, Search.Progress progress,
        ChannelWriter<ValueTask<PooledList<int>>> resultChannelWriter,
        ChannelWriter<(IMemoryOwner<byte> memory, int startLine, int EndLine, ValueTaskSource<PooledList<int>>)> searchTasksChannelWriter,
        CancellationToken cancellationToken)
    {
        Trace("LoadBuffersWorker started");
        var sourceLineIndex = logModelFacade.LineIndex;
        await using var stream = logModelFacade.LogFile.OpenForSequentialRead();
        await foreach (var (start, count) in
                       sourceLineIndex.FetchRanges(cancellationToken))
        {
            var current = start;
            while (current < start + count && !cancellationToken.IsCancellationRequested)
            {
                var end = Math.Min(current + MaxSearchSizeLines, start + count) - 1;
                    
                var (firstLineOffset, _) = sourceLineIndex.GetLine(current);
                var (lastLineOffset, lastLineLength) = sourceLineIndex.GetLine(end);
                
                var size = lastLineOffset + lastLineLength - firstLineOffset;
                var memoryOwner = MemoryPool<byte>.Shared.Rent((int) size);

                _ = stream.Seek(firstLineOffset, SeekOrigin.Begin);
                _ = stream.Read(memoryOwner.Memory.Span);
  
                var source = new ValueTaskSource<PooledList<int>>();
                var resultTask = new ValueTask<PooledList<int>>(source, 0);
                await searchTasksChannelWriter.WriteAsync((memoryOwner, current, end, source), cancellationToken);
                await resultChannelWriter.WriteAsync(resultTask, cancellationToken);
                
                var loadedCount = sourceLineIndex.Count;
                var totalCountEstimate = loadedCount / logModelFacade.LoadProgress * 100.0;
               
                progress.Value = current / totalCountEstimate;
                current += MaxSearchSizeLines;
            }
        }

        searchTasksChannelWriter.Complete();
        resultChannelWriter.Complete();
        Trace("LoadBuffersWorker finished, buffersChannel completed");
    }

    private async Task SearchInBufferWorker(Regex regex, LineIndex lineIndex, Encoding encoding,
        ChannelReader<(IMemoryOwner<byte> memory, int startLine, int EndLine, ValueTaskSource<PooledList<int>>)>
            searchTasksChannelReader,
        CancellationToken cancellationToken)
    {
        Trace("SearchInBufferWorker started");
        var counter = 0;
        var stringBuffer = _stringPool.Rent(2048);

        await foreach (var (memory, startLine, endLine, resultTaskSource) in searchTasksChannelReader.ReadAllAsync(
                           cancellationToken))
        {
            if (counter == 0)
            {
                Trace("SearchInBufferWorker: processing first data");
            }

            counter++;
            var result = SearchInBufferRegex(regex, memory.Memory, startLine, endLine,
                lineIndex, encoding, ref stringBuffer, cancellationToken);

            memory.Dispose();
            resultTaskSource.SetResult(result);
        }
        _stringPool.Return(stringBuffer);
        Trace($"SearchInBufferWorker finished, processed {counter} chunks of data");
    }

    private PooledList<int> SearchInBufferRegex(Regex regex, Memory<byte> memory, int start, int end, 
        LineIndex lineIndex, Encoding encoding, ref string stringBuffer, CancellationToken cancellationToken)
    {
        var result = new PooledList<int>();

        var lineCount = end - start + 1;
        using var offsets = new PooledList<(long offset, int length)>(lineCount);
        lineIndex.Fetch(start, offsets.AllocateSpan(lineCount));

        var (firstLineOffset, firstLineLength) = offsets[0];
            
        var currentLineOffset = firstLineOffset;
        var currentLineLength = firstLineLength;
        var index = start;
        var memorySpan = memory.Span;
        do {
            var charCount = encoding.GetMaxCharCount(currentLineLength);

            if (stringBuffer.Length < charCount)
            {
                _stringPool.Return(stringBuffer);
                stringBuffer = _stringPool.Rent(charCount);
            }

            var bytes =
                memorySpan.Slice((int) (currentLineOffset - firstLineOffset), currentLineLength);
                   
            int stringLength;
            unsafe
            {
                fixed (char* stringPointer = stringBuffer.AsSpan())
                {
                    var chars = new Span<char>(stringPointer, charCount);
                    stringLength = encoding.GetChars(bytes, chars);
                }
            }

            if (regex.IsMatch(stringBuffer.AsSpan()[..stringLength]))
            {
                result.Add(index);
            }

            index++;

            if (index > end)
                break;
                    
            (currentLineOffset, currentLineLength) = offsets[index - start];
                    
        } while (!cancellationToken.IsCancellationRequested);
        
        return result;
    }
    
    private async Task ProcessSearchResultsWorker(SearchLineIndex lineIndex,
        Indexer sourceIndexer,
        SubIndexer searchIndexer,
        ChannelReader<ValueTask<PooledList<int>>> searchResultsChannelReader,
        TaskCompletionSource taskCompletionSource,
        CancellationToken cancellationToken)
    {
        Trace("ProcessSearchResultsWorker started");

        await foreach (var searchResult in searchResultsChannelReader.ReadAllAsync(cancellationToken))
        {
            using var result = await searchResult;
            if (result.Count == 0)
                continue;
            
            foreach (var index in result)
            {
                var indexKeyNum = sourceIndexer.GetIndexKeyNum(index);
                var currentSearchResultLineNumber = lineIndex.Add(index);
                searchIndexer.Add(indexKeyNum, currentSearchResultLineNumber);
            }
        }
        
        taskCompletionSource.SetResult();
        Trace("ProcessSearchResultsWorker finished");
    }
}