using System;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        public static LogMetaInformation CreateTextFileMetaInformation()
        {
            return new LogMetaInformation(@"^(?'Text'.*)", new[]{"Text"}, 
                Array.Empty<int>(), Array.Empty<string>());
        }

        public string[] Transformations { get; }

        public string[] FieldNames { get; }

        public int[] IndexedFieldNumbers { get; }

        public LogMetaInformation(LogFormat logFormat)
            : this(logFormat.Regex, 
                logFormat.FieldNames,
                logFormat.IndexedFieldNumbers,
                logFormat.Transformations)
        {
        }

        public LogMetaInformation(string lineRegex, string [] fieldNames, int[] indexedFieldNumbers, string[] transformations)
        {
            LineRegex = lineRegex;
            FieldNames = fieldNames;
            ComponentCount = FieldNames.Length;
            IndexedFieldNumbers = indexedFieldNumbers;
            Transformations = transformations;
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