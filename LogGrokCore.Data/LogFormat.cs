using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LogFormat
    {
        public string Regex { get; set; } = string.Empty;
        
        public string[] FieldsOrder { get; set; } = Array.Empty<string>();

        public string[] IndexedFields { get; set; } = Array.Empty<string>();

        public int[] IndexedFieldNumbers 
        {
            get
            {
                var regex = new Regex(Regex);
                return IndexedFields.Select(n => regex.GroupNumberFromName(n) - 1).ToArray();
            }
        }
    }
}