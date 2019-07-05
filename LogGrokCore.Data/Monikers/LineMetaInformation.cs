using System;
using System.Runtime.InteropServices;

namespace LogGrokCore.Data.Monikers
{
    public readonly ref struct LineMetaInformation
    {
        private readonly Span<int> _placeholder;
        private readonly ParsedLineComponents _lineComponents;
        private readonly int _componentCount;
        
        public static unsafe LineMetaInformation Get(char* pointer, int componentCount)
        {
            return new LineMetaInformation(new Span<int>(pointer, GetSizeInts(componentCount)), componentCount);
        }

        public LineMetaInformation(Span<int> placeholder, int componentCount)
        {
            _placeholder = placeholder;
            _componentCount = componentCount;
            _lineComponents = new ParsedLineComponents(_placeholder.Slice(1));
        }

        public ref int LineLength => ref _placeholder[0];
        public ParsedLineComponents ParsedLineComponents => _lineComponents;
        
        public static int GetSizeInts(int componentCount) => 1 + componentCount * 2;
        public static int GetSizeChars(int componentCount) => GetSizeInts(componentCount) * sizeof(int) / sizeof(char);

        public int TotalSizeWithPayloadChars => GetSizeChars(_componentCount) 
                                                + ParsedLineComponents.GetAllCompnentsLength(_componentCount);

        public int TotalSizeInts => GetSizeInts(_componentCount);
    }
}