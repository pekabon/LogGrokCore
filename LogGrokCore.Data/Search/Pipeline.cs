using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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

        var buffersChannel = Channel.CreateBounded<(IMemoryOwner<byte> memory,
            int startLine, int EndLine)>(new BoundedChannelOptions(Environment.ProcessorCount)
        {
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = true,
            SingleWriter = true,
            SingleReader = false
        });

        var searchResultsChannel = Channel.CreateBounded<ValueTask<PooledList<int>>>(
            new BoundedChannelOptions(Environment.ProcessorCount)
            {
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = true,
                SingleWriter = false,
                SingleReader = true
            });

        
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
                    Trace($"Operation canceled: {actionExpression}.");
                }
            } , cancellationToken);
        }

        var processSearchResultsCompletionSource = new TaskCompletionSource();
        var workers = new List<Task>
        {
            StartAsyncTask(() => LoadBuffersWorker(_logModelFacade, progress, buffersChannel.Writer, cancellationToken)),
            StartAsyncTask(() => ProcessSearchResultsWorker(lineIndex, 
                 sourceIndexer, searchIndexer, searchResultsChannel.Reader, 
                 processSearchResultsCompletionSource, cancellationToken))

        };
        
        var searchInBufferCompletion = searchResultsChannel.StartProducers(channelWriter =>
            SearchInBufferWorker(_regex, sourceLineIndex, encoding,
                buffersChannel.Reader, channelWriter, cancellationToken), _searchWorkersCount);

        await searchInBufferCompletion;
        await Task.WhenAll(workers.ToArray());
        await processSearchResultsCompletionSource.Task;
        Trace($"Search finished, spent: {DateTime.Now-timestamp}");
    }

    private async Task LoadBuffersWorker(LogModelFacade logModelFacade, Search.Progress progress,
        ChannelWriter<(IMemoryOwner<byte> memory, int startLine, int endLine)> buffersQueue,
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
                await buffersQueue.WriteAsync((memoryOwner, current, end), cancellationToken);
                
                var loadedCount = sourceLineIndex.Count;
                var totalCountEstimate = loadedCount / logModelFacade.LoadProgress * 100.0;
               
                progress.Value = current / totalCountEstimate;
                current += MaxSearchSizeLines;
            }
        }

        buffersQueue.Complete();
        Trace("LoadBuffersWorker finished, buffersChannel completed");
    }
    
    private async Task SearchInBufferWorker(
        Regex regex, LineIndex lineIndex, Encoding encoding,
        ChannelReader<(IMemoryOwner<byte> memory, int startLine, int EndLine)> buffersReader,
        ChannelWriter<ValueTask<PooledList<int>>> resultChannelWriter,
        CancellationToken cancellationToken)
    {
        Trace("SearchInBufferWorker started");
        var stringBuffer = _stringPool.Rent(2048);
        var counter = 0;
        await foreach(var (memory, startLine, endLine) in buffersReader.ReadAllAsync(cancellationToken))
        {
            if (counter == 0)
            {
                Trace("SearchInBufferWorker: processing first data");
            }

            counter++;
            var source = new ValueTaskSource<PooledList<int>>();
            var resultTask = new ValueTask<PooledList<int>>(source, 0);
            await resultChannelWriter.WriteAsync(resultTask, cancellationToken);
            var result = SearchInBufferRegex(regex, memory.Memory, startLine, endLine,
                lineIndex, encoding, ref stringBuffer, cancellationToken);
            memory.Dispose();
            source.SetResult(result);            
        }
        
        _stringPool.Return(stringBuffer);
        Trace($"SearchInBufferWorker finished, processed {counter} chunks of data");
    }
    
    PooledList<int> SearchInBufferRegex(Regex regex, Memory<byte> memory, int start, int end, 
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

            // TODO: there is no regex.IsMatch function that accepts string length
            // get rid of regex.Match 
            if (regex.Match(stringBuffer, 0, stringLength).Success)
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

        var lastIndex = -1;
        await foreach (var searchResult in searchResultsChannelReader.ReadAllAsync(cancellationToken))
        {
            using var result = await searchResult;
            if (result.Count == 0)
                continue;

            
            foreach (var index in result)
            {
                if (index <= lastIndex)
                    Console.Write("");

                lastIndex = index;
                
                var indexKeyNum = sourceIndexer.GetIndexKeyNum(index);
                var currentSearchResultLineNumber = lineIndex.Add(index);
                searchIndexer.Add(indexKeyNum, currentSearchResultLineNumber);
            }
        }
        
        taskCompletionSource.SetResult();
        Trace("ProcessSearchResultsWorker finished");
    }
}