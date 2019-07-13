namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        public string[] FieldNames { get; }

        public int[] IndexedFieldNumbers { get; }

        public LogMetaInformation(string lineRegex, int[] indexedFieldNumbers)
        {
            LineRegex = lineRegex;
            FieldNames = new[] {"Time", "Thread", "Severity", "Component", "Text"};
            ComponentCount = FieldNames.Length;
            IndexedFieldNumbers = indexedFieldNumbers;
        }

        public string LineRegex { get; }

        public int ComponentCount { get; }
    }
}