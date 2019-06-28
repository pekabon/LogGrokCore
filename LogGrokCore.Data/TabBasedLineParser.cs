using System;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class TabBasedLineParser : ILineParser
    {
        private readonly Regex _regex;
        private readonly int _componentCount;

        public TabBasedLineParser(LogMetaInformation logMetaInformation)
        {
            _regex = logMetaInformation.LineRegex;
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

            lineMeta.LineLength = length;

            var componentStart = 0;
            var stringSpan = input.AsSpan(beginning, length);
            
            for (var idx = 0; idx < _componentCount - 1; idx++)
            {
                var currentSpan = stringSpan.Slice(componentStart);
                var tabIndex = currentSpan.IndexOf('\t');
                
                lineMeta.ComponentStart(idx) = componentStart;
                lineMeta.ComponentLength(idx) = tabIndex;
                if (tabIndex < 0)
                    Console.Write("");
                
                componentStart += tabIndex + 1;
            }

            lineMeta.ComponentStart(_componentCount - 1) = componentStart;
            lineMeta.ComponentLength(_componentCount - 1) = length - componentStart;
            return true;
        }
    }
}