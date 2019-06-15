namespace LogGrokCore.Data
{

    public interface ILineIndex
    {
        int Count { get; }

        (long offset, int lenghth) GetLine(int index);
        void Add(long lineStart);
        void Finish(int lastLength);
    }
}