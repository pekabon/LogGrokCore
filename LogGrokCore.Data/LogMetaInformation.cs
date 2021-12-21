using System;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        private readonly LogFormat _logFormat;

        public static LogMetaInformation CreateTextFileMetaInformation()
        {
            var plainTextLogFormat = new LogFormat()
            {
                Regex = @"^(?'Text'.*)",
                IndexedFields = Array.Empty<string>(),
                Transformations = Array.Empty<string>(),
                XorMask = 0
            };
            
            return new LogMetaInformation(plainTextLogFormat);
        }

        public string[] Transformations => _logFormat.Transformations;

        public string[] FieldNames => _logFormat.FieldNames;

        public int[] IndexedFieldNumbers => _logFormat.IndexedFieldNumbers;

        public string LineRegex => _logFormat.Regex;

        public int ComponentCount => FieldNames.Length;

        public byte XorMask => _logFormat.XorMask;

        public LogMetaInformation(LogFormat logFormat)
        {
            _logFormat = logFormat;
        }

        private int GetFieldIndexByName(string fieldName) => Array.IndexOf(FieldNames, fieldName);

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
    }
}