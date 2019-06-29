namespace LogGrokCore.Data
{
    public interface ILineParser
    {
        bool TryParse(string input, int beginning, int length, in LineMetaInformation lineMeta);

        ParseResult Parse(string input);
    }
}