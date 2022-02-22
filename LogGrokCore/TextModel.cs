using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data;
using Microsoft.Toolkit.HighPerformance;
using YamlDotNet.Core.Tokens;

namespace LogGrokCore;

public class TextModel : IReadOnlyList<StringRange>
{
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
        if (jsonIntervals.Count == 0)
        {
            var textLines = source.Tokenize().ToList();
            if (textLines.Count > 1)
            {
                _textLines = textLines.Select(c => TextOperations.Normalize(c, viewSettings)).ToList();
            }

            _sourceText = StringRange.FromString(TextOperations.Normalize(source, 
                ApplicationSettings.Instance().ViewSettings));
        }
        else
        {
            var text = TextOperations.Normalize(
                TextOperations.FormatInlineJson(source, jsonIntervals.AsSpan()),
                viewSettings);

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
            
            var (textLines, collapsibleRanges) = GetCollapsibleRanges(text, rootCollapsedLineTextSubstitutions);
            _textLines = textLines;
            CollapsibleRanges = new List<(int start, int length)>();
            foreach (((int start, int length) range , StringRange substitution) r in collapsibleRanges)
            {
                CollapsibleRanges.Add(r.range);
                if (!r.substitution.IsEmpty)
                {
                    _substitutions ??= new Dictionary<int, StringRange>();
                    _substitutions[r.range.start] = r.substitution;
                }
            }
        }
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
            for (var  i =0; i<lines?.Count; i++)

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

        while (jsonIntervals.TryPop(out var interval))
        {
            AddInterval(interval);
                
            var ((start, length), _) = interval;
            if (length > 2)
            {
                var offset = start + 1;
                var groups =
                    TextOperations.GetBracedGroups(source.AsSpan(offset, length - 2))
                        .Select(g => (g.start + offset, g.length));
                foreach (var group in groups)
                {
                    jsonIntervals.Push((group, StringRange.Empty));
                }
            }
        }

        return (lines, result);
    }
}