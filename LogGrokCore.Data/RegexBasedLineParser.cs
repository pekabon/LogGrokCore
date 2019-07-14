using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data
{
    public class RegexBasedLineParser : ILineParser
    {
        private readonly Regex _regex;
        private readonly int _componentCount;
        private readonly int[] _fieldsToStore;

        public RegexBasedLineParser(LogMetaInformation logMetaInformation, 
            bool onlyIndexed = false)
        {
            _regex = new Regex(logMetaInformation.LineRegex, 
                onlyIndexed ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.Singleline);
            _componentCount = logMetaInformation.ComponentCount;
            _fieldsToStore = onlyIndexed
                ? logMetaInformation.IndexedFieldNumbers
                : Enumerable.Range(0, _componentCount).ToArray();
        }

        public ParseResult Parse(string input)
        {
            var placeholder = new int[LineMetaInformation.GetSizeInts(_componentCount)];
            if (!TryParse(input, 0, input.Length,
                new LineMetaInformation(placeholder.AsSpan(), _componentCount).ParsedLineComponents))
                throw new InvalidOperationException();
            return new ParseResult(_componentCount, placeholder);
        }

        public bool TryParse(string input, int beginning, int length,
            in ParsedLineComponents parsedLineComponents)
        {
            var match = _regex.Match(input, beginning, length);
            if (!match.Success)
                return false;

            var groups = match.Groups;

            var index = 0;
            var lastComponentStart = 0;
            foreach (var fieldToStore in _fieldsToStore)
            {
                var componentLength = groups[fieldToStore + 1].Length;
                lastComponentStart =
                    componentLength != 0 ? groups[fieldToStore + 1].Index - beginning : lastComponentStart;

                Debug.Assert(lastComponentStart >= 0);
                Debug.Assert(componentLength >= 0);

                parsedLineComponents.ComponentStart(index) = lastComponentStart;
                parsedLineComponents.ComponentLength(index) = componentLength;
                index++;
            }

            return true;
        }
    }
}