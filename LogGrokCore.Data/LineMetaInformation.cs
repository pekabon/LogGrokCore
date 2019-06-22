using System;

namespace LogGrokCore.Data
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
        }

        public ref int LineLength => ref _placeholder[0];

        public ref int ComponentStart(int index) => ref _placeholder[index * 2 + 1];

        public ref int ComponentLength(int index) => ref _placeholder[index * 2 + 2];

        public int AllComponentsLength => ComponentStart(_componentCount - 1) + ComponentLength(_componentCount - 1);

        public static int GetSizeInts(int componentCount) => 1 + componentCount * 2;
        public static int GetSizeChars(int componentCount) => GetSizeInts(componentCount) * sizeof(int) / sizeof(char);

        public int TotalSize => GetSizeChars(_componentCount) + AllComponentsLength;
    }
}