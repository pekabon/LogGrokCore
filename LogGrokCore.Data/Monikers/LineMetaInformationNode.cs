using System;

namespace LogGrokCore.Data.Monikers
{
    public readonly ref struct LineMetaInformationNode
    {
        private readonly Span<int> _placeholder;

        public int NextNodeOffset
        {
            get => _placeholder[0];
            set => _placeholder[0] = value;
        }

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
        }

        public int TotalSizeCharsAligned => Align.Get(2 + LineMetaInformation.TotalSizeWithPayloadChars, Alignment);

        private const int Alignment = 2;
    }
}