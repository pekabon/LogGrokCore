using System;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data;
using Microsoft.Toolkit.HighPerformance;

namespace LogGrokCore
{
    public class LinePartViewModel : ViewModelBase
    {
        List<(int start, int length)>? _collapsibleRegions;
        private const int MaxLines = 20;

        public LinePartViewModel(string source)
        {
            void SetTrimmedProperties()
            {
                var (text, trimmedLinesCount) = TextOperations.TrimLines(source, MaxLines);
                Text = TextOperations.Normalize(text, ApplicationSettings.Instance().ViewSettings);
                TrimmedLinesCount = trimmedLinesCount;
                IsTrimmedLinesHidden = true;
            }

            void SetUntrimmedProperties()
            {
                Text = source;
                TrimmedLinesCount = 0;
                IsTrimmedLinesHidden = false;
            }

            ExpandCommand = new DelegateCommand(() =>
            {
                SetUntrimmedProperties();
                RaiseAllPropertiesChanged();
            });

            CollapseCommand = new DelegateCommand(() =>
            {
                SetTrimmedProperties();
                RaiseAllPropertiesChanged();
            });


            FullText = source;            
            var jsonIntervals = TextOperations.GetJsonRanges(source).ToList();

            _collapsibleRegions = null;
            if (jsonIntervals.Count > 0)
            {
                Text = TextOperations.Normalize(
                    TextOperations.FormatInlineJson(source, jsonIntervals.AsSpan()),
                    ApplicationSettings.Instance().ViewSettings);

                _collapsibleRegions = GetCollapsibleRegions(Text);
            }
            else Text = FullText;
            
            
            //SetTrimmedProperties();
            IsCollapsible = TrimmedLinesCount > 0;
        }

        public List<(int start, int length)>? CollapsibleRanges => _collapsibleRegions;
        private List<(int start, int length)> GetCollapsibleRegions(string source)
        {
            var lines = source.Tokenize().ToList();
            List<(int start, int length)> result = new();
            
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

            void AddInterval((int start, int length) interval)
            {
                var (start, length) = interval;
                var startLine = GetLineNumber(start);
                var endLine = GetLineNumber(start + length);
                var lengthLines = endLine - startLine;
                if (lengthLines > 0) 
                    result.Add((startLine, lengthLines));
            }

            var jsonIntervals = new Stack<(int start, int length)>(TextOperations.GetJsonRanges(source));
        
            while (jsonIntervals.TryPop(out var interval))
            {
                AddInterval(interval);
                
                var (start, length) = interval;
                if (length > 2)
                {
                    var offset = start + 1;
                    var groups =
                        TextOperations.GetBracedGroups(source.AsSpan(offset, length - 2))
                            .Select(g => (g.start + offset, g.length));
                    foreach (var group in groups)
                    {
                        jsonIntervals.Push(group);
                    }
                }
            }

            return result;
        }

        public bool IsCollapsible { get; }

        public bool IsTrimmedLinesHidden { get; private set; }

        public DelegateCommand ExpandCommand { get; }

        public DelegateCommand CollapseCommand { get; }
        
        public string? Text { get; private set; }
        
        public int TrimmedLinesCount { get; private set; }

        public string? FullText { get; }
    }
}