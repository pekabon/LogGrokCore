using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        public Regex LineRegex { get; }

        public int ComponentCount { get; }
    }
}