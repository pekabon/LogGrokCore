using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogGrokCore.Data.IndexTree;

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

        public class SearchLineIndex : ILineIndex
        {
            private readonly IndexTree<int, SimpleLeaf<int>> _searchResult 
                = new IndexTree<int, SimpleLeaf<int>>(32, 
                value => new SimpleLeaf<int>(value, 0));

            private readonly ILineIndex _sourceLineIndex;
            public int Count => _searchResult.Count;
            
            public SearchLineIndex(ILineIndex sourceLineIndex)
            {
                _sourceLineIndex = sourceLineIndex;
            }

            public (long offset, int length) GetLine(int index)
            {
                var sourceIndex = _searchResult[index];
                return _sourceLineIndex.GetLine(sourceIndex);
            }

            public void Fetch(int start, Span<(long offset, int length)> values)
            {
                for (var i = start; i < start + values.Length; i++)
                {
                    values[i] = _sourceLineIndex.GetLine(i);
                }
            }

            public void Add(int sourceLineIndex)
            {
                _searchResult.Add(sourceLineIndex);
            }

        }

        public static (Progress, SearchLineIndex) CreateSearchIndex(
            Stream stream, Encoding encoding,
            LineIndex sourceLineIndex, Regex regex)
        {
            var lineIndex = new SearchLineIndex(sourceLineIndex);
            
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

                } while (index < end);
            }

            var progress = new Progress();
            Task.Run(async () =>
            {
                var totalCount = sourceLineIndex.Count;
                await foreach (var (start, count) in sourceLineIndex.FetchRanges(MaxSearchSizeLines))
                {
                    
                    ProcessLines(start, start + count - 1);
                    progress.Value = (double)totalCount / (start + count);
                }

                progress.IsFinished = true;
            });

            return (progress, lineIndex);
        }
    }
}