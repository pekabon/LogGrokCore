using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Data.Search
{
    public class Pipeline
    {
        private const int MaxSearchSizeLines = 512;

        private StringPool _stringPool = new();
        public async Task StartSearch(
            Regex regex,
            LogModelFacade logModelFacade,
            SearchLineIndex lineIndex,
            Indexer searchIndexer,
            Search.Progress progress,
            CancellationToken cancellationToken)
        {
            
            //Trace.TraceInformation($"Searching '{regex}'; Current range: {start}, count={count}");
            var encoding = logModelFacade.LogFile.Encoding;


            var sourceLineIndex = logModelFacade.LineIndex;
            var sourceIndexer = logModelFacade.Indexer;

            var buffersQueue = new BlockingCollection<(IMemoryOwner<byte> memory, 
                int startLine, int EndLine)>(Environment.ProcessorCount);
            var searchResultsQueue = new BlockingCollection<Task<PooledList<int>>>(
                Math.Max(1, Environment.ProcessorCount - 1));
           
            var loadTask = Task.Factory.StartNew(async () => await LoadTask(buffersQueue, logModelFacade, cancellationToken), 
                    cancellationToken);
            var passBufferTask  = Task.Factory.StartNew(() => PassBuffersToSearch(regex, sourceLineIndex, encoding,
                buffersQueue, searchResultsQueue, cancellationToken), 
                    cancellationToken);


            foreach (var searchResult in searchResultsQueue.GetConsumingEnumerable(cancellationToken))
            {
                var result = await searchResult;
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
                searchResult.Dispose();

                var loadedCount = sourceLineIndex.Count;
                var totalCountEstimate = loadedCount / logModelFacade.LoadProgress * 100.0;
                progress.Value = (double) lineNum / totalCountEstimate;
            }

            Task.WaitAll(new[] { loadTask, passBufferTask }, cancellationToken: cancellationToken);
            progress.IsFinished = true;
        }

        void PassBuffersToSearch(
            Regex regex, LineIndex lineIndex, Encoding encoding,
            BlockingCollection<(IMemoryOwner<byte> memory, int startLine, int endLine)> buffersQueue,
            BlockingCollection<Task<PooledList<int>>> searchResultsQueue,
            CancellationToken cancellationToken)
        {            
            foreach (var (memory, startLine, endLine) in buffersQueue.GetConsumingEnumerable(cancellationToken))
            {
                var searchTask = Task.Factory.StartNew(() =>
                    SearchInBuffer(regex, memory.Memory, startLine, endLine,
                        lineIndex, encoding, cancellationToken), cancellationToken)
                    .ContinueWith(t =>
                    {
                        memory.Dispose();
                        return t.Result;
                    }, cancellationToken);
                searchResultsQueue.Add(searchTask, cancellationToken);
            }
            searchResultsQueue.CompleteAdding();
        }
        
        PooledList<int> SearchInBuffer(Regex regex, Memory<byte> memory, int start, int end, 
            LineIndex lineIndex, Encoding encoding, CancellationToken cancellationToken)
        {

            var result = new PooledList<int>();
            var tempString = _stringPool.Rent(2048); 
//                new string('\0', 2048);

            var lineCount = end - start + 1;
            using var offsets = new PooledList<(long offset, int length)>(lineCount);
            lineIndex.Fetch(start, offsets.AllocateSpan(lineCount));

            var (firstLineOffset, firstLineLength) = offsets[0];
            var (lastLineOffset, lastLineLength) = offsets[end - start];
            
            var currentLineOffset = firstLineOffset;
            var currentLineLength = firstLineLength;
            var index = start;
            var memorySpan = memory.Span;
            do {
                var charCount = encoding.GetMaxCharCount(currentLineLength);

                if (tempString.Length < charCount)
                {
                    _stringPool.Return(tempString);
                    tempString = _stringPool.Rent(charCount);
                }

                var bytes =
                    memorySpan.Slice((int) (currentLineOffset - firstLineOffset), currentLineLength);
                   
                var stringLength = 0;
                unsafe
                {
                    fixed (char* stringPointer = tempString.AsSpan())
                    {
                        var chars = new Span<char>(stringPointer, charCount);
                        stringLength = encoding.GetChars(bytes, chars);
                    }
                }

                // TODO: there is no regex.IsMatch function that accepts string length
                // get rid of regex.Match 
                if (regex.Match(tempString, 0, stringLength).Success)
                {
                    result.Add(index);
                }

                index++;

                if (index > end)
                    break;
                    
                (currentLineOffset, currentLineLength) = offsets[index - start];
                    
            } while (!cancellationToken.IsCancellationRequested);

            _stringPool.Return(tempString);
            return result;
        }
                

        async Task LoadTask(BlockingCollection<(IMemoryOwner<byte> memory, 
                int startLine, int EndLine)> buffersQueue, LogModelFacade logModelFacade,
            CancellationToken cancellationToken)
        {
            
            var sourceIndexer = logModelFacade.Indexer;
            var sourceLineIndex = logModelFacade.LineIndex;
            
            SearchLineIndex lineIndex = new(sourceLineIndex); // searchResultLineNumber -> originalLogLineNumber mapping
            var searchIndexer = new Indexer();                // components -> searchResultLineNumber
            
            await using var stream = logModelFacade.LogFile.OpenForSequentialRead();
            await foreach (var (start, count) in
                           sourceLineIndex.FetchRanges(cancellationToken))
            {
                var loadedCount = sourceLineIndex.Count;
                Trace.TraceInformation($"Load: start = {start}, count = {count}, start+count = {start+count}, loadedCount ={loadedCount}");
                
                //var loadedCount = sourceLineIndex.Count;
                var totalCountEstimate = loadedCount / logModelFacade.LoadProgress * 100.0;
                        
                var sourceIndexedSequence = sourceIndexer.GetIndexedSequenceFrom(start);
                using var sourceIndexedSequenceEnumerator = sourceIndexedSequence.GetEnumerator();
                var current = start;
                while (current < start + count && !cancellationToken.IsCancellationRequested)
                {

                    var end = Math.Min(current + MaxSearchSizeLines, start + count) - 1;
                    
                    var (firstLineOffset, firstLineLength) = sourceLineIndex.GetLine(current);
                    var (lastLineOffset, lastLineLength) = sourceLineIndex.GetLine(end);
                
                    var size = lastLineOffset + lastLineLength - firstLineOffset;
                    var memoryOwner = MemoryPool<byte>.Shared.Rent((int) size);

                    _ = stream.Seek(firstLineOffset, SeekOrigin.Begin);
                    _ = stream.Read(memoryOwner.Memory.Span);
                    buffersQueue.Add((memoryOwner, current, end), cancellationToken);
                    current += MaxSearchSizeLines;
                }
            }
            buffersQueue.CompleteAdding();
        }

}

    public static class Search
    {
        private const int MaxSearchSizeLines = 512;
        private const double Throttle = 0.01;
       
        public class Progress
        {
            public double Value
            {
                get => _value;
                set
                {
                    _value = value;
                    if (value - _lastReportedValue > Throttle)
                        ReportNewValue(value);
                }
            }

            public bool IsFinished
            {
                get => _isFinished;
                set
                {
                    _isFinished = value;
                    IsFinishedChanged?.Invoke();
                }
            }

            private void ReportNewValue(double value)
            {
                Changed?.Invoke(value);
                _lastReportedValue = value;
            }

            public event Action? IsFinishedChanged;

            public event Action<double>? Changed;

            private double _value;
            private double _lastReportedValue;
            private bool _isFinished;
        }

        public static (Progress, Indexer, SearchLineIndex) CreateSearchIndex(
            LogModelFacade logModelFacade,
            Regex regex,
            CancellationToken cancellationToken)
        {
            var encoding = logModelFacade.LogFile.Encoding;
            var sourceIndexer = logModelFacade.Indexer;
            var sourceLineIndex = logModelFacade.LineIndex;
            
            SearchLineIndex lineIndex = new(sourceLineIndex); // searchResultLineNumber -> originalLogLineNumber mapping
            var searchIndexer = new Indexer();                // components -> searchResultLineNumber
                 
            // void ProcessLines(Stream stream, IEnumerator<Indexer.LineAndKey> lineAndKeyEnumerator, int start, int end)
            // {
            //     var (firstLineOffset, firstLineLength) = sourceLineIndex.GetLine(start);
            //     var (lastLineOffset, lastLineLength) = sourceLineIndex.GetLine(end);
            //     
            //     var size = lastLineOffset + lastLineLength - firstLineOffset;
            //     using var memoryOwner = MemoryPool<byte>.Shared.Rent((int) size);
            //     var memorySpan = memoryOwner.Memory.Span;
            //
            //     _ = stream.Seek(firstLineOffset, SeekOrigin.Begin);
            //     _ = stream.Read(memorySpan);
            //
            //     var index = start; // index: originalLogLineNumber
            //
            //     var currentLineOffset = firstLineOffset;
            //     var currentLineLength = firstLineLength;
            //
            //     var tempString = new string('\0', 2048);
            //
            //     var lineCount = end - start + 1;
            //     using var offsets = new PooledList<(long offset, int length)>(lineCount);
            //     sourceLineIndex.Fetch(start, offsets.AllocateSpan(lineCount));
            //
            //     do
            //     {
            //         var enumerateResult = lineAndKeyEnumerator.MoveNext();
            //         Debug.Assert(enumerateResult);
            //         var (lineNum, indexKey) = lineAndKeyEnumerator.Current;
            //         Debug.Assert(lineNum == index);
            //         
            //         var charCount = encoding.GetMaxCharCount(currentLineLength);
            //
            //         if (tempString.Length < charCount)
            //         {
            //             tempString = new string('\0', Pow2Roundup(charCount));
            //         }
            //
            //         var bytes =
            //             memorySpan.Slice((int) (currentLineOffset - firstLineOffset), currentLineLength);
            //        
            //         var stringLength = 0;
            //         unsafe
            //         {
            //             fixed (char* stringPointer = tempString.AsSpan())
            //             {
            //                 var chars = new Span<char>(stringPointer, charCount);
            //                 stringLength = encoding.GetChars(bytes, chars);
            //             }
            //         }
            //
            //         // TODO: there is no regex.IsMatch function that accepts string length
            //         // get rid of regex.Match 
            //         if (regex.Match(tempString, 0, stringLength).Success)
            //         {
            //             var currentSearchResultLineNumber = lineIndex.Add(index);
            //             searchIndexer.Add(indexKey, currentSearchResultLineNumber);
            //         }
            //
            //         index++;
            //
            //         if (index > end)
            //             break;
            //         
            //         (currentLineOffset, currentLineLength) = offsets[index - start];
            //         
            //     } while (!cancellationToken.IsCancellationRequested);
            // }

            var progress = new Progress();
            // Task.Run(async () =>
            //     {
            //         await using var stream = logModelFacade.LogFile.OpenForSequentialRead();
            //         await foreach (var (start, count) in
            //             sourceLineIndex.FetchRanges(cancellationToken))
            //         {
            //             Trace.TraceInformation($"Searching '{regex}'; Current range: {start}, count={count}");
            //             var loadedCount = sourceLineIndex.Count;
            //             var totalCountEstimate = loadedCount / logModelFacade.LoadProgress * 100.0;
            //             
            //             var sourceIndexedSequence = sourceIndexer.GetIndexedSequenceFrom(start);
            //             
            //             using var sourceIndexedSequenceEnumerator = sourceIndexedSequence.GetEnumerator();
            //             var current = start;
            //             while (current < start + count && !cancellationToken.IsCancellationRequested)
            //             {
            //                 ProcessLines(stream, sourceIndexedSequenceEnumerator, 
            //                     current, Math.Min(current + MaxSearchSizeLines, start + count) - 1);
            //                 progress.Value = (double) current / totalCountEstimate;
            //                 current += MaxSearchSizeLines;
            //             }
            //         }
            //     }, cancellationToken)
            //     .ContinueWith(_ =>
            //     {
            //         searchIndexer.Finish();
            //         return progress.IsFinished = true;
            //     }, cancellationToken);

            var pipeline = new Pipeline();
            Task.Run(async () =>
                    await pipeline.StartSearch(regex, logModelFacade, lineIndex, searchIndexer, progress,
                        cancellationToken), cancellationToken)
                .ContinueWith(_ =>
                {
                    searchIndexer.Finish();
                    return progress.IsFinished = true;
                }, cancellationToken);
       
            
            return (progress, searchIndexer, lineIndex);
        }
        
        private static int Pow2Roundup (int x)
        {
            if (x < 0)
                return 0;
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x+1;
        }
    }
}