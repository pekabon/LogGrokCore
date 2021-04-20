using System;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        public static LogMetaInformation CreateKlLogMetaInformation()
        {
            return new(
                @"^(?'Time'\d{2}\:\d{2}\:\d{2}\.\d{3})\t(?'Thread'0x[0-9a-fA-F]+)\t(?'Severity'\w+)\t(?'Component'[\w\.]+)?\t?(?'Message'.*)",
                new[] {"Time", "Thread", "Severity", "Component", "Text"},
                new[] {1, 2, 3}
            );
        }
        
        public static LogMetaInformation CreateTextFileMetaInformation()
        {
            return new LogMetaInformation(@"^(?'Text'.*)", new[]{"Text"}, Array.Empty<int>());
        }

        public string[] FieldNames { get; }

        public int[] IndexedFieldNumbers { get; }

        public LogMetaInformation(LogFormat logFormat)
            : this(logFormat.Regex, 
                logFormat.FieldsOrder,
                logFormat.IndexedFieldNumbers)
        {
        }

        public LogMetaInformation(string lineRegex, string [] fieldNames, int[] indexedFieldNumbers)
        {
            LineRegex = lineRegex;
            FieldNames = fieldNames;
            ComponentCount = FieldNames.Length;
            IndexedFieldNumbers = indexedFieldNumbers;
        }

        public int GetFieldIndexByName(string fieldName) => Array.IndexOf(FieldNames, fieldName);

        public int GetIndexedFieldIndexByName(string fieldName) =>
            GetIndexedFieldIndexByFieldIndex(GetFieldIndexByName(fieldName));
        
        public int GetIndexedFieldIndexByFieldIndex(int index) =>
            Array.IndexOf(IndexedFieldNumbers, index);

        public bool IsFieldIndexed(string fieldName)
        {
            var index = Array.IndexOf(FieldNames, fieldName);
            if (index < 0) return false;
            return Array.IndexOf(IndexedFieldNumbers, index) >= 0;
        }

        public string LineRegex { get; }

        public int ComponentCount { get; }
    }
}