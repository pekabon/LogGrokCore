using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data.Index
{
    public interface IIndexedLinesProvider : IItemProvider<int>
    {
        int GetIndexByValue(int value);
    }
}