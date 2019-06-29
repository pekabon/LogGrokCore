using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        public string[] FieldNames { get; }

        public LogMetaInformation(string lineRegex)
        {
            LineRegex = lineRegex;
            FieldNames = new[] {"Time", "Thread", "Severity", "Component", "Text"};
            ComponentCount = FieldNames.Length;
        }

        public string LineRegex { get; }

        public int ComponentCount { get; }
    }
}