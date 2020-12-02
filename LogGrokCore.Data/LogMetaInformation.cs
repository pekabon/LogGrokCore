using System;

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