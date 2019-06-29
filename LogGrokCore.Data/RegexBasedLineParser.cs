using System;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class RegexBasedLineParser : ILineParser
    {
        private readonly Regex _regex;
        private readonly int _componentCount;

        public RegexBasedLineParser(LogMetaInformation logMetaInformation)
        {
            _regex = new Regex(logMetaInformation.LineRegex, RegexOptions.Compiled);
            _componentCount = logMetaInformation.ComponentCount;
        }

        public ParseResult Parse(string input)
        {
            var placeholder = new int[LineMetaInformation.GetSizeInts(_componentCount)];
            
            if (!TryParse(input, 0, input.Length, new LineMetaInformation(placeholder.AsSpan(), _componentCount)))
                throw new InvalidOperationException();
            return new ParseResult(_componentCount, placeholder);
        }

        public bool TryParse(string input, int beginning, int length, in LineMetaInformation lineMeta)
        {
            var match = _regex.Match(input, beginning, length);
            if (!match.Success)
                return false;

            var groups = match.Groups;
            for (var idx = 1; idx <= _componentCount; idx++)
            {
                lineMeta.ComponentStart(idx - 1) = groups[idx].Index;
                lineMeta.ComponentLength(idx - 1) = groups[idx].Length;
            }

            return true;
        }
    }
}