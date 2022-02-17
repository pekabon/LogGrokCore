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
    private Dictionary<int, StringRange>? _substitutions;

    public List<(int start, int length)>? CollapsibleRanges { get; }

    public StringRange GetCollapsedTextSubstitution(int index)
    {
        if (_substitutions?.TryGetValue(index, out var result) ?? false)
        {
            return result;
        }
        
        return StringRange.Empty;
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
    
    
    public TextModel(string source)
    {
        var jsonIntervals = TextOperations.GetJsonRanges(source).ToList();
        if (jsonIntervals.Count == 0)
        {
            _sourceText = StringRange.FromString(TextOperations.Normalize(source, 
                ApplicationSettings.Instance().ViewSettings));
        }
        else
        {
            var text = TextOperations.Normalize(
                TextOperations.FormatInlineJson(source, jsonIntervals.AsSpan()),
                ApplicationSettings.Instance().ViewSettings);

            var rootCollapsedLineTextSubstitutions = jsonIntervals.Select(interval 
                =>
            {
                var range = new StringRange()
                    { SourceString = source, Start = interval.start, Length = interval.length };
                return range.IsSingleLine() ? range : StringRange.Empty;
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