using System;
using System.Text;

namespace LogGrokCore
{
    public static class TextOperations
    {
        private const int MaxLineLength = 4096;
        private const string Ellipsis = "...";
       
        public static string Normalize(string source)
        {
            return (source.Length < MaxLineLength ? source : FormatLongString(source.TrimEnd('\0'))).TrimEnd();
        }

        private static string FormatLongString(string source)
        {
            var lines = 
                source.Split(Environment.NewLine);

            if (lines.Length == 1 && lines[0].Length <= MaxLineLength)
                return source;

            var sb = new StringBuilder();

            for (var idx = 0; idx < lines.Length; idx++)
            {
                var line = lines[idx];
                if (line.Length <= MaxLineLength)
                {
                    sb.Append(line);
                }
                else
                {
                    sb.Append(line, 0, MaxLineLength);
                    sb.Append(Ellipsis);
                }
                
                if (idx != lines.Length - 1)
                    sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}