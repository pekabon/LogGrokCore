﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LogFormat
    {
        public string Regex { get; set; } = string.Empty;

        public string[] FieldNames => GetFieldNames(Regex); 

        public string[] IndexedFields { get; set; } = Array.Empty<string>();

        public string[] Transformations { get; set; } = Array.Empty<string>();

        public byte XorMask { get; set; } = 0;

        public bool IsCorrect()
        {
            try
            {
                _ = new Regex(Regex);
                foreach (var transformation in Transformations)
                {
                    _ = new Regex(transformation);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Invalid regex: {Regex}, exception: {e}");
                return false;
            }

            return true;
        }

        public int[] IndexedFieldNumbers 
        {
            get
            {
                var regex = new Regex(Regex);
                return IndexedFields.Select(n => regex.GroupNumberFromName(n) - 1).ToArray();
            }
        }

        private string[] GetFieldNames(string regexString)
        {
            if (_fieldNames != null) return _fieldNames;
            var groupNames = new Regex(regexString).GetGroupNames();
            _fieldNames = groupNames[1..].ToArray();

            return _fieldNames;
        }

        private string[]? _fieldNames;
    }
}