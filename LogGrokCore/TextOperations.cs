using System;
using System.IO;
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
            for (int i = 0; i <= source.Length / settings.SizeBigLine; ++i)
            {
                if (i != 0 )
                    sb.Append("\n");
                if(i * settings.SizeBigLine + settings.SizeBigLine < source.Length)
                    sb.Append(source.Substring(i * settings.SizeBigLine, settings.SizeBigLine));
                else
                    sb.Append(source.Substring(i * settings.SizeBigLine));
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

        public static bool IsExistsInlineJson(string text)
        {
            if (String.IsNullOrEmpty(text) || text.IndexOf("\n") + 1 != text.Length)
            {
                return false;
            }

            int first = text.IndexOf("{");
            int last = text.LastIndexOf("}");
            if (first == -1 || last == -1)
                return false;

            return IsInlineJson(text.Substring(first, last - first + 1));
        }

        public static string ExpandInlineJson(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return text;
            }

            try
            {
                int first = text.IndexOf("{");
                int last = text.LastIndexOf("}");
                if (first == -1 || last == -1)
                    return text;

                var rawStr = text.Substring(first, last - first + 1);
                return text.Substring(0, first) + FormatJsonText(rawStr) + text.Substring(last + 1);
            }
            catch (JsonException)
            {
                return text;
            }
        }

        private static bool IsInlineJson(string jsonString)
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
            MemoryStream memoryStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true, 
                                                                                         Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                doc.WriteTo(writer);
            }
            return new System.Text.UTF8Encoding()
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
                span = source[(offset)..];
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