namespace LogGrokCore.Data
{
    public interface ILineIndex
    {
        int Count { get; }

        (long offset, int lenghth) GetLine(int index);
        int Add(long lineStart);
        void Finish(int lastLength);
    }
}