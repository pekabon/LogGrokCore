using System;
using System.Buffers;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LogGrokCore.Data.IndexTree;

namespace LogGrokCore.Data.Search
{
    public class Search
    {
        private static StringPool _searchStringPool = new StringPool();
        private const int _searchBufferSize = 16384;
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

        public unsafe ILineIndex? Create(
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
                    var charCount = encoding.GetMaxCharCount(firstLineLength);
                    
                    var tempString = _searchStringPool.Rent(charCount);
                    var bytes = 
                        memorySpan.Slice((int) (currentLineOffset - firstLineOffset), currentLineLength);

                    fixed (char* stringPointer = tempString.AsSpan())
                    {
                        var chars = new Span<char>(stringPointer, charCount);
                        var stringLength = encoding.GetChars(bytes, chars);
                    }

                    if (regex.IsMatch(tempString))
                    {
                        lineIndex.Add(index);
                    }
                    
                    _searchStringPool.Return(tempString);
                    index++;

                } while (index < end);
            }

            var startIndex = 0;
            var endIndex = Math.Min(startIndex + maxSearchSizeLines, lineIndex.Count - 1);

            while (startIndex < lineIndex.Count)
            {
                ProcessLines(startIndex, endIndex);
                startIndex = endIndex + 1;
            }


            return lineIndex;
        }
    }
}