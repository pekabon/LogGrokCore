using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data;
using Microsoft.Toolkit.HighPerformance;

namespace LogGrokCore;

public class TextModel : IReadOnlyList<StringRange>
{
    private const int CollapseToLines = 20;
    
    private readonly List<StringRange>? _textLines;
    private readonly StringRange? _sourceText;
    private readonly Dictionary<int, StringRange>? _substitutions;
    private Dictionary<int, (int start, int length)>? _indexedCollapsibleRanges;

    public int UniqueId { get; }

    public List<(int start, int length)>? CollapsibleRanges { get; }

    public StringRange GetCollapsedTextSubstitution(int index)
    {
        if (_textLines is not { } textLines || CollapsibleRanges is not { } collapsibleRanges)
        {
            throw new InvalidOperationException();
        }

        if (_substitutions?.TryGetValue(index, out var result) ?? false)
        {
            return result;
        }

        _indexedCollapsibleRanges ??= collapsibleRanges.ToDictionary(
            static kv => kv.start, static kv => kv);

        var collapsibleRange = _indexedCollapsibleRanges[index];
        var collapsedText = string.Concat(_textLines.Skip(collapsibleRange.start)
            .Take(collapsibleRange.length).Select(
                (s, i) => i == 0 ? s.ToString().TrimEnd() : s.ToString().Trim()));

        return StringRange.FromString(collapsedText);
    }

    public IEnumerator<StringRange> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _textLines?.Count ?? 1;

    public StringRange this[int index]
    {
        get
        {
            var textLines = _textLines;

            if (textLines == null)
            {
                return (index, _sourceText) switch
                {
                    (0, { } value) => value,
                    _ => throw new InvalidOperationException(),
                };
            }

            return textLines[index];
        }
    }

    public TextModel(int uniqueId, string source)
    {
        UniqueId = uniqueId;
        var jsonIntervals = TextOperations.GetJsonRanges(source).ToList();
        var viewSettings = ApplicationSettings.Instance().ViewSettings;
        if (jsonIntervals.Count != 0)
        {
            (_textLines, _substitutions, CollapsibleRanges) = GetJsonProperties(source, jsonIntervals);
        }
        else
        {
            var textLines = source.Tokenize().ToList();
            if (textLines.Count > 1)
            {
                _textLines = textLines.Select(c => TextOperations.Normalize(c, viewSettings)).ToList();
            }

            if (textLines.Count > CollapseToLines)
            {
                var linesToCollapse = textLines.Count - CollapseToLines;
                CollapsibleRanges = new List<(int start, int length)>
                {
                    (CollapseToLines, linesToCollapse)
                };

                _substitutions = new Dictionary<int, StringRange>()
                {
                    { CollapseToLines, StringRange.FromString($"More {linesToCollapse} lines >>>") }
                };

            }
            
            
            _sourceText = StringRange.FromString(TextOperations.Normalize(source,
                ApplicationSettings.Instance().ViewSettings));
        }
    }

    private (List<StringRange> textLines,
        Dictionary<int, StringRange>? substitutions,
        List<(int start, int length)>?)
        GetJsonProperties(string source, List<(int start, int length)> jsonIntervals)
    {
        var text = TextOperations.FormatInlineJson(source, jsonIntervals.AsSpan());

        static StringRange GetContainingLine(StringRange range)
        {
            foreach (var line in range.SourceString.Tokenize())
            {
                if (line.Start <= range.Start && line.End >= range.End)
                    return line;
            }

            throw new InvalidOperationException();
        }

        var rootCollapsedLineTextSubstitutions = jsonIntervals.Select(interval
            =>
        {
            var range = new StringRange()
                { SourceString = source, Start = interval.start, Length = interval.length };
            return range.IsSingleLine() ? GetContainingLine(range) : StringRange.Empty;
        }).ToList();

        var (textLines, collapsibleRangesWithSubstitutions) =
            GetCollapsibleRanges(text, rootCollapsedLineTextSubstitutions);

        var collapsibleRanges = new List<(int start, int length)>();
        Dictionary<int, StringRange>? substitutions = null;
        foreach (((int start, int length) range, StringRange substitution) r in collapsibleRangesWithSubstitutions)
        {
            collapsibleRanges.Add(r.range);
            if (r.substitution.IsEmpty) continue;
            substitutions ??= new Dictionary<int, StringRange>();
            substitutions[r.range.start] = r.substitution;
        }

        return (textLines, substitutions, collapsibleRanges);
    }

    private (List<StringRange> textLines,
        List<((int start, int length), StringRange)> ranges) GetCollapsibleRanges(string source,
            List<StringRange> rootCollapsedLineTextSubstitutions)
    {
        var lines = source.Tokenize().ToList();
        List<((int start, int length), StringRange collapsedTextSubstitution)> result = new();
        var ranges = TextOperations.GetJsonRanges(source).ToList();
        var rootIntervals = ranges.Select((r, i) => ((start: r.start, length: r.length),
            rootCollapsedLineTextSubstitutions[i])).ToList();
        var jsonIntervals = new Stack<((int start, int length), StringRange collapsedTextSubstitution)>(rootIntervals);

        int GetLineNumber(int position)
        {
            for (var i = 0; i < lines?.Count; i++)

            {
                var stringRange = lines[i];
                if (position >= stringRange.Start && position <= stringRange.Start + stringRange.Length)
                    return i;
            }

            throw new InvalidOperationException();
        }

        void AddInterval(((int start, int length), StringRange collapsedTextSubstitution) interval)
        {
            var ((start, length), substitution) = interval;
            var startLine = GetLineNumber(start);
            var endLine = GetLineNumber(start + length);
            var lengthLines = endLine - startLine + 1;
            if (lengthLines > 1)
                result.Add(((startLine, lengthLines), substitution));
        }

        var bracesStack = new Stack<(char brace, int position)>();

        int PopChar(char ch)
        {
            if (!bracesStack!.TryPeek(out var prev) || prev.brace != ch)
                throw new InvalidOperationException();

            bracesStack.Pop();
            return prev.position;
        }

        foreach (var jsonInterval in jsonIntervals)
        {
            var ((offset, length), _) = jsonInterval;

            var position = 0;
            var span = source.AsSpan(offset, length);

            while (position < length)
            {
                position = span[position..].IndexOfAny("{}[]") + position;
                if (position < 0)
                    break;
                switch (span[position])
                {
                    case '{':
                    case '[':
                        bracesStack.Push((span[position], position));
                        break;
                    case '}':
                    case ']':
                        var start = PopChar(span[position] == ']' ? '[' : '{');
                        var len = position - start;
                        AddInterval(((start + offset, len), StringRange.Empty));
                        break;
                }

                position++;
            }
        }

        return (lines, result);
    }
}