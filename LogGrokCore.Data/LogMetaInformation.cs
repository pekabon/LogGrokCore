using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        public string[] FieldNames { get; }

        public LogMetaInformation(Regex lineRegex, int componentCount)
        {
            LineRegex = lineRegex;
            ComponentCount = componentCount;
            FieldNames = new[] {"Time", "Thread", "Text"};
        }

        public Regex LineRegex { get; }

        public int ComponentCount { get; }
    }
}