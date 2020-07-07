using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LogGrokCore.Data.Search
{
    public static class Search
    {
        private static readonly StringPool SearchStringPool = new StringPool();
        private const int MaxSearchSizeLines = 256;
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

        public static (Progress, SearchLineIndex) CreateSearchIndex(
            Stream stream, Encoding encoding,
            LineIndex sourceLineIndex, Regex regex,
            CancellationToken cancellationToken)
        {
            SearchLineIndex lineIndex = new SearchLineIndex(sourceLineIndex);

            void ProcessLines(int start, int end)
            {
                var (firstLineOffset, firstLineLength) = sourceLineIndex.GetLine(start);
                var (lastLineOffset, lastLineLength) = sourceLineIndex.GetLine(end);
                var size = lastLineOffset + lastLineLength - firstLineOffset;
                using var memoryOwner = MemoryPool<byte>.Shared.Rent((int) size);
                var memorySpan = memoryOwner.Memory.Span;

                _ = stream.Seek(firstLineOffset, SeekOrigin.Begin);
                _ = stream.Read(memorySpan);

                var index = start;

                var currentLineOffset = firstLineOffset;
                var currentLineLength = firstLineLength;

                do
                {
                    var charCount = encoding.GetMaxCharCount(currentLineLength);

                    var tempString = SearchStringPool.Rent(charCount);
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
                        lineIndex.Add(index);
                    }

                    SearchStringPool.Return(tempString);
                    index++;

                    (currentLineOffset, currentLineLength) = sourceLineIndex.GetLine(index);
                } while (index < end && !cancellationToken.IsCancellationRequested);
            }

            var progress = new Progress();
            Task.Run(async () =>
                {

                    await foreach (var (start, count) in
                        sourceLineIndex.FetchRanges(cancellationToken))
                    {
                        var totalCount = sourceLineIndex.Count;

                        var current = start;
                        while (current < start + count && !cancellationToken.IsCancellationRequested)
                        {
                            ProcessLines(current, Math.Min(current + MaxSearchSizeLines, start + count - 1));
                            progress.Value = (double) current / totalCount;
                            current += MaxSearchSizeLines;
                        }
                    }
                })
                .ContinueWith(_ =>
                {
                    return progress.IsFinished = true;
                }, cancellationToken);
        

        return (progress, lineIndex);
        }
    }
}