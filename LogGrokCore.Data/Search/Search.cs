using System;
using System.Buffers;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogGrokCore.Data.IndexTree;

namespace LogGrokCore.Data.Search
{
    public class Search
    {
        private static StringPool _searchStringPool = new StringPool();
        private const int maxSearchSizeLines = 256; 

        private class SearchLineIndex : ILineIndex
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

            public void Add(int sourceLineIndex)
            {
                _searchResult.Add(sourceLineIndex);
            }
        }

        public ILineIndex Create(
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
                    
                    var tempString = _searchStringPool.Rent(charCount);
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
                    
                    _searchStringPool.Return(tempString);
                    index++;
                    
                    (currentLineOffset, currentLineLength) = sourceLineIndex.GetLine(index);

                } while (index < end);
            }

            Task.Run(async () =>
            {
                await foreach (var (start, count) in sourceLineIndex.FetchRanges(maxSearchSizeLines))
                {
                    ProcessLines(start, start + count - 1);
                }

            });

            return lineIndex;
        }
    }
}