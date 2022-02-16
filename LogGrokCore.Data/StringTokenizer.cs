using System;
using System.Collections.Generic;

namespace LogGrokCore.Data
{
    public static class StringTokenizer
    {
        private static readonly char[] Crlf = new[] { '\r', '\n' };

        public static IEnumerable<StringRange> Tokenize(this string source)
        {
            return Tokenize(StringRange.FromString(source));
        }

        public static IEnumerable<StringRange> Tokenize(this StringRange source)
        {
            var currentIndex = 0;
            var firstCrLfIndex = -1;
            while (currentIndex < source.Length)
            {
                var crlfIndex = source.Span[currentIndex..].IndexOfAny(Crlf);
                if (crlfIndex == -1)
                {
                    var len = source.Length - currentIndex;
                    if (len > 0)
                    {
                        yield return new StringRange
                            { SourceString = source.SourceString, 
                                Start = source.Start + currentIndex, Length = source.Length - currentIndex };
                        yield break;
                    }
                }

                if (crlfIndex == 0)
                {
                    currentIndex++;
                    continue;
                }

                yield return new StringRange()
                    { SourceString = source.SourceString, Start = source.Start + currentIndex, Length = crlfIndex};
                currentIndex += crlfIndex + 1;
            }
        }
    }
}