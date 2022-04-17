using System;

namespace LogGrokCore.Data.Monikers
{
    public readonly ref struct ParsedLineComponents
    {
        private readonly Span<int> _placeholder;
        
        public ParsedLineComponents(Span<int> placeholder)
        {
            _placeholder = placeholder;
        }

        public ReadOnlySpan<char> GetComponent(ReadOnlySpan<char> input, int index)
        {
            var componentStart = ComponentStart(index);
            var componentLength = ComponentLength(index);
            return input.Slice(componentStart, componentLength);
        }
        
        public ref int ComponentStart(int index) => ref _placeholder[index * 2];
        
        public ref int ComponentLength(int index) => ref _placeholder[index * 2 + 1];

        public int GetAllComponentsLength(int componentCount)
        {
            if (componentCount == 0) return 0;
            var lastComponentStart = ComponentStart(componentCount - 1);
            var lastComponentLength = ComponentLength(componentCount - 1);
            return lastComponentStart + lastComponentLength;
        }
    }
}