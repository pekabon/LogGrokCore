using System;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class TablBasedLineParser
    {
        private Regex _regex;
        private int _componentCount;

        public TablBasedLineParser(Regex regex, int componentCount)
        {
            _regex = regex;
            _componentCount = componentCount;
        }

        public bool Parse(string input, int beginning, int length, in LineMetaInformation lineMeta)
        {
            var match = _regex.Match(input, beginning, length);
            if (!match.Success)
                return false;

            var componentStart = 0;
            var stringSpan = input.AsSpan(beginning, length);
            for (var idx = 0; idx < _componentCount; idx++)
            {
                var tabIndex = stringSpan.IndexOf('\t');

                lineMeta.LineLength = length;
                lineMeta.ComponentStart(idx) = componentStart;
                lineMeta.ComponentLength(idx) = tabIndex;

                componentStart += tabIndex + 1;
                stringSpan = stringSpan.Slice(componentStart);
            }
            return true;
        }
    }
}