namespace LogGrokCore.Data
{
    public interface ILineIndex
    {
        int Count { get; }

        (long offset, int length) GetLine(int index);
        int Add(long lineStart);
        void Finish(int lastLength);
    }
}