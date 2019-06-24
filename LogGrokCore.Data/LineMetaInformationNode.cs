using System;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public readonly ref struct LineMetaInformationNode
    {
        private readonly Span<int> _placeholder;

        public ref int NextNodeOffset => ref _placeholder[0];

        public LineMetaInformation LineMetaInformation { get; }

        public static int GetSizeChars(int lineComponentCount) => GetSizeInts(lineComponentCount) * sizeof(int) / sizeof(char);

        public static int GetSizeInts(int lineComponentCount) => 1 + LineMetaInformation.GetSizeInts(lineComponentCount);

        public unsafe static LineMetaInformationNode Get(char* pointer, int componentCount)
        {
            return new LineMetaInformationNode(new Span<int>(pointer, GetSizeInts(componentCount)), componentCount);
        }

        public LineMetaInformationNode(Span<int> placeholder, int componentCount)
        {
            _placeholder = placeholder;
            LineMetaInformation = new LineMetaInformation(placeholder.Slice(1), componentCount);
            NextNodeOffset = -1;
        }

        public int TotalSizeCharsAligned => Align.Get(1 + LineMetaInformation.TotalSizeWithPayloadChars, Alignment);

        private const int Alignment = 2;
    }
}