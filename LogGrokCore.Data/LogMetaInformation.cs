using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        public string[] FieldNames { get; }

        public LogMetaInformation(Regex lineRegex)
        {
            LineRegex = lineRegex;
            FieldNames = new[] {"Time", "Thread", "Severity", "Component", "Text"};
            ComponentCount = FieldNames.Length;
        }

        public Regex LineRegex { get; }

        public int ComponentCount { get; }
    }
}