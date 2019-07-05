using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data
{
    public interface ILineParser
    {
        bool TryParse(string input, int beginning, int length, in ParsedLineComponents components);

        ParseResult Parse(string input);
    }
}