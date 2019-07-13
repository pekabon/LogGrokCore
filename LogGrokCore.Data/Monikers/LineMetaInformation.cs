using System;
using System.Runtime.CompilerServices;


namespace LogGrokCore.Data.Monikers
{
    public readonly ref struct LineMetaInformation
    {
        private readonly Span<int> _placeholder;
        private readonly int _componentCount;
        
        public static unsafe LineMetaInformation Get(char* pointer, int componentCount)
        {
            return new LineMetaInformation(new Span<int>(pointer, GetSizeInts(componentCount)), componentCount);
        }

        public LineMetaInformation(Span<int> placeholder, int componentCount)
        {
            _placeholder = placeholder;
            _componentCount = componentCount;
            ParsedLineComponents = new ParsedLineComponents(_placeholder.Slice(1));
        }
       
        public ref int LineOffsetFromBufferStart => ref _placeholder[0];

        public ParsedLineComponents ParsedLineComponents { get; }

        public static int GetSizeInts(int componentCount) => 1 /*LineOffsetFromBufferStart*/ + componentCount * 2;
        public static int GetSizeChars(int componentCount) => GetSizeInts(componentCount) * sizeof(int) / sizeof(char);

        public int TotalSizeWithPayloadChars => GetSizeChars(_componentCount) 
                                                + ParsedLineComponents.GetAllCompnentsLength(_componentCount);
        public int TotalSizeWithPayloadCharsAligned => Align.Get(TotalSizeWithPayloadChars, 2);
    }
}