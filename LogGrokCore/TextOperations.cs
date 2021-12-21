using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace LogGrokCore
{
    public static class TextOperations
    {
        private const string Ellipsis = "...";

        public static string Normalize(string source, ViewSettings settings)
        {
            return (source.Length < settings.SizeBigLine ? source : FormatLongString(source.TrimEnd('\0'), settings)).TrimEnd();
        }
        private static string FormatLongString(string source, ViewSettings settings)
        {
            var lines = 
                source.Split(Environment.NewLine);

            if (lines.Length == 1 && lines[0].Length <= settings.SizeBigLine)
                return source;

            var sb = new StringBuilder();

            for (var idx = 0; idx < lines.Length; idx++)
            {
                var line = lines[idx];
                if (line.Length <= settings.SizeBigLine)
                {
                    sb.Append(line);
                }
                else
                {
                    if(settings.BigLine == ViewSettings.ViewBigLine.Break)
                    {
                        sb.Append(BreakLongString(line, settings));
                    }
                    else
                    {
                        sb.Append(line, 0, settings.SizeBigLine);
                        sb.Append(Ellipsis);
                    }
                }
                
                if (idx != lines.Length - 1)
                    sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        private static string BreakLongString(string source, ViewSettings settings)
        {
            var sb = new StringBuilder();
            for (var i = 0; i <= source.Length / settings.SizeBigLine; ++i)
            {
                if (i != 0 )
                    sb.Append("\n");
                if(i * settings.SizeBigLine + settings.SizeBigLine < source.Length)
                    sb.Append(source.AsSpan(i * settings.SizeBigLine, settings.SizeBigLine));
                else
                    sb.Append(source.AsSpan(i * settings.SizeBigLine));
            }
            return sb.ToString();
        }

        public static int CountLines(string source)
        {
            return CountLines(source.AsSpan());
        }

        public static (string resultString, int linesTrimmed) TrimLines(string source, uint maxLines)
        {
            var sourceSpan = source.AsSpan();
            var toTrimOffset = SkipLines(sourceSpan, maxLines);
            return (new string(sourceSpan[..toTrimOffset]), CountLines(sourceSpan[toTrimOffset..]));
        }

        public static IEnumerable<(int start, int length)> GetJsonRanges(string source)
        {
            return GetBracedGroups(source.AsSpan()).Where(interval => 
                IsValidJson(source.Substring(interval.start, interval.length)));
        }
        
        public static IEnumerable<(int start, int length)> GetBracedGroups(ReadOnlySpan<char> sourceText)
        {
            List<(int, int)>? result = null;
            var offset = 0;
            var text = sourceText;

            IEnumerable<(int start, int length)> MakeResult(List<(int, int)>? resultValue)
            {
                return resultValue ?? Enumerable.Empty<(int start, int length)>();
            }

            while (text.Length > 0)
            {
                var openBraceIndex = text.IndexOf('{') + offset;
                if (openBraceIndex == -1) return MakeResult(result);
                var nextBraceIndex = -1;
                var counter = 0;

                offset = openBraceIndex + 1;
                text = sourceText[offset..];
                
                while (counter >= 0)
                {
                    var localNextBraceIndex = text.IndexOfAny("{}");
                    if (localNextBraceIndex == -1) return MakeResult(result);
                    nextBraceIndex = localNextBraceIndex + offset;
                    if (sourceText[nextBraceIndex] == '{')
                        counter++;
                    else
                        counter--;

                    offset = nextBraceIndex + 1;
                    text = sourceText[offset..];
                }

                if (nextBraceIndex > 0)
                {
                    result ??= new List<(int, int)>(8);
                    result.Add((openBraceIndex, offset - openBraceIndex));
                }

                text = sourceText[offset..];
            }

            return MakeResult(result);
        }

        public static string FormatInlineJson(ReadOnlySpan<char> text, ReadOnlySpan<(int start, int length)> jsonIntervals)
        {
            if (jsonIntervals.Length == 0)
                return text.ToString();

            var firstStart = jsonIntervals[0].start;
            var firstLength = jsonIntervals[0].length;
            StringBuilder stringBuilder = new();
            stringBuilder.Append(text[..firstStart]);
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(FormatJsonText(text.Slice(firstStart, firstLength).ToString()));
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(FormatInlineJson(text[(firstStart + firstLength)..], jsonIntervals[1..]));
            return stringBuilder.ToString();
        }

        private static bool IsValidJson(string jsonString)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonString, new JsonDocumentOptions { AllowTrailingCommas = true });
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        private static string FormatJsonText(string jsonString)
        {
            using var doc = JsonDocument.Parse(jsonString, new JsonDocumentOptions { AllowTrailingCommas = true });
            var memoryStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream, 
                       new JsonWriterOptions { Indented = true, 
                                               Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                doc.WriteTo(writer);
            }
            return new UTF8Encoding()
                .GetString(memoryStream.ToArray());
        }
        
        private static int SkipLines(ReadOnlySpan<char> source, uint linesToSkip)
        {
            if (linesToSkip == 0) return 0;
            var offset = 0;
            var span = source;
            while (true)
            {
                var indexOfLf = span.IndexOf('\n');
                if (indexOfLf == -1)
                    return offset + span.Length;
                
                offset += indexOfLf + 1;
                if (linesToSkip == 1) return offset;
                span = source[offset..];
                linesToSkip -= 1;
            }
        }

        private static int CountLines(ReadOnlySpan<char> source)
        {
            if (source.IsEmpty) return 0;
            var indexof = source.IndexOf('\n');
            return indexof > 0 ? 1 + CountLines(source[(indexof + 1)..]) : 1;
        }
    }
}