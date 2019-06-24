namespace LogGrokCore.Data
{
    public interface ILineParser
    {
        bool Parse(string input, int beginning, int length, in LineMetaInformation lineMeta);
    }
}