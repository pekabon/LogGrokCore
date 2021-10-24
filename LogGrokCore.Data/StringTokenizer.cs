using System;
using System.Collections.Generic;

namespace LogGrokCore.Data
{
    public readonly struct StringRange
    {
        public string SourceString { get; init; }

        public int Start { get; init; }

        public int Length { get; init; }

        public ReadOnlySpan<char> Span => SourceString.AsSpan(Start, Length);

        public override string ToString()
        {
            return new string(Span);
        }
    }

    public static class StringTokenizer
    {
        private static readonly char[] Crlf = new[] { '\r', '\n' };
        public static IEnumerable<StringRange> Tokenize(this string source)
        {
            var str = source.AsSpan();
            var currentIndex = 0;
            while (currentIndex < source.Length)
            {
                var crlfIndex = source.AsSpan(currentIndex).IndexOfAny(Crlf);
                if (crlfIndex == -1)
                {
                    var len = source.Length - currentIndex;
                    if (len > 0)
                    {
                        yield return new StringRange
                            { SourceString = source, Start = currentIndex, Length = source.Length - currentIndex };
                        yield break;
                    }
                }

                if (crlfIndex == 0)
                {
                    currentIndex++;
                    continue;
                }

                yield return new StringRange()
                    { SourceString = source, Start = currentIndex, Length = crlfIndex };
                currentIndex += crlfIndex + 1;
            }
        }
    }
}