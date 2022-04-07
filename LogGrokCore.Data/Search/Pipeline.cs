using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Data.Search;

public class Pipeline
{
    private const int MaxSearchSizeLines = 1024;
    private readonly int _searchWorkersCount = Math.Max(Environment.ProcessorCount - 1, 1);
    private readonly StringPool _stringPool = new();
    public async Task StartSearch(
        Regex regex,
        LogModelFacade logModelFacade,
        SearchLineIndex lineIndex,
        Indexer searchIndexer,
        Search.Progress progress,
        CancellationToken cancellationToken)
    {
        var encoding = logModelFacade.LogFile.Encoding;
        var sourceLineIndex = logModelFacade.LineIndex;
        var sourceIndexer = logModelFacade.Indexer;

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

        var workers = new List<Task>
        {
            LoadBuffersWorker(logModelFacade, progress, buffersChannel.Writer, cancellationToken),
            ProcessSearchResultsWorker(lineIndex, 
                 sourceIndexer, searchIndexer, searchResultsChannel.Reader, cancellationToken)

        };
        for (var i = 0; i < _searchWorkersCount; i++)
        {
            workers.Add(SearchInBufferWorker(regex, sourceLineIndex, encoding,
                buffersChannel.Reader, searchResultsChannel.Writer, cancellationToken));
        }
       
        await Task.WhenAll(workers.ToArray());
    }

    private static async Task LoadBuffersWorker(LogModelFacade logModelFacade, Search.Progress progress,
        ChannelWriter<(IMemoryOwner<byte> memory, int startLine, int endLine)> buffersQueue,
        CancellationToken cancellationToken)
    {
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

        _ = buffersQueue.TryComplete();
    }
    
    private async Task SearchInBufferWorker(
        Regex regex, LineIndex lineIndex, Encoding encoding,
        ChannelReader<(IMemoryOwner<byte> memory, int startLine, int EndLine)> buffersReader,
        ChannelWriter<ValueTask<PooledList<int>>> resultChannelWriter,
        CancellationToken cancellationToken)
    {            
        var stringBuffer = _stringPool.Rent(2048); 
        await foreach(var (memory, startLine, endLine) in buffersReader.ReadAllAsync(cancellationToken))
        {
            var source = new ValueTaskSource<PooledList<int>>();
            var resultTask = new ValueTask<PooledList<int>>(source, 0);
            await resultChannelWriter.WriteAsync(resultTask, cancellationToken);
            var result = SearchInBuffer(regex, memory.Memory, startLine, endLine,
                lineIndex, encoding, ref stringBuffer, cancellationToken);
            memory.Dispose();
            source.SetResult(result);            
        }
        
        _stringPool.Return(stringBuffer);
        _ = resultChannelWriter.TryComplete();
    }
    
    PooledList<int> SearchInBuffer(Regex regex, Memory<byte> memory, int start, int end, 
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
    
    private static async Task ProcessSearchResultsWorker(
        SearchLineIndex lineIndex,
        Indexer sourceIndexer,
        Indexer searchIndexer,
        ChannelReader<ValueTask<PooledList<int>>> searchResultsChannelReader,
        CancellationToken cancellationToken)
    {
        await foreach (var searchResult in searchResultsChannelReader.ReadAllAsync(cancellationToken))
        {
            using var result = await searchResult;
            if (result.Count == 0)
                continue;

            var sourceIndexedSequence = sourceIndexer.GetIndexedSequenceFrom(result[0]);
            using var lineAndKeyEnumerator = sourceIndexedSequence.GetEnumerator();
            var enumerateResult = lineAndKeyEnumerator.MoveNext();
            Debug.Assert(enumerateResult);
            var (lineNum, indexKey) = lineAndKeyEnumerator.Current;

            foreach (var index in result)
            {
                while (lineNum != index)
                {
                    enumerateResult = lineAndKeyEnumerator.MoveNext();
                    Debug.Assert(enumerateResult);
                    (lineNum, indexKey) = lineAndKeyEnumerator.Current;
                }

                Debug.Assert(lineNum == index);

                var currentSearchResultLineNumber = lineIndex.Add(index);
                searchIndexer.Add(indexKey, currentSearchResultLineNumber);
            }
        }
    }
}