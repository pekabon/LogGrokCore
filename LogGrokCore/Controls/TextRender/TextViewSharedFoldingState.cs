using System;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Controls.TextRender;

public class TextViewSharedFoldingState
{
    private const int DefaultMaxVisibleLines = 20;
    private int _cleanupCounter = 0;

    private Func<IEnumerable<(int start, int length)>, int, HashSet<int>> _defaultSettingsFactory = CreateDefaultSettings;
    private Dictionary<int, WeakReference<TextView>> _clientRefs = new();
    
    public HashSet<int>? this[int id]
    {
        get => _collapsedLineIndicesByUniqueId.TryGetValue(id, out var result) ? 
            result : null;
        set => _collapsedLineIndicesByUniqueId[id] = value;
    }

    public void CollapseAll() => SetDefaultSettingsFactoryAndReset(CreateSettingsForAllCollapsed);

    public void ExpandAll() => SetDefaultSettingsFactoryAndReset(CreateSettingsForAllExpanded);

    public void ResetAllToDefault() => SetDefaultSettingsFactoryAndReset(CreateDefaultSettings);

    public HashSet<int> GetDefaultSettings(
        IEnumerable<(int start, int length)> collapsibleRanges, 
        int totalLineCount)
    {
        return _defaultSettingsFactory(collapsibleRanges, totalLineCount);
    }

    public HashSet<int> GetDefaultFoldingSettings(IEnumerable<(int start, int length)> collapsibleRanges,
        int totalLineCount)
    {
        return CreateDefaultSettings(collapsibleRanges, totalLineCount);
    }

    private void SetDefaultSettingsFactoryAndReset(
        Func<IEnumerable<(int start, int length)>, int, HashSet<int>> defaultSettingsFactory)
    {
        _collapsedLineIndicesByUniqueId.Clear();
        _defaultSettingsFactory = defaultSettingsFactory;
        ResetAllExistingTextViews();
    }
    
    private void ResetAllExistingTextViews()
    {
        foreach (var (_, clientRef) in _clientRefs)
        {
            if (clientRef.TryGetTarget(out var textView))
                textView.ResetFoldingToDefault();
        }
    }

    public void Register(TextView textView)
    {
        _clientRefs[textView.GetHashCode()] = new WeakReference<TextView>(textView);
        _cleanupCounter++;
        if (_cleanupCounter % 256 == 0)
        {
            CleanupClientRefs();
        }
    }
    
    public void Unregister(TextView textView)
    {
        _clientRefs.Remove(textView.GetHashCode());
    }

    private void CleanupClientRefs()
    {
        _clientRefs = _clientRefs.Where(c => c.Value.TryGetTarget(out _))
            .ToDictionary(pair => pair.Key, pair=> pair.Value);
    }
    
    private class HierarchicalInterval
    {
        public (int start, int length) Range { get; init; }
        public List<HierarchicalInterval> SubIntervals { get; } = new();

        public int MinLength => Range.length - SubIntervals.Sum(h => h.Range.length - 1);

        public override string ToString()
        {
            return $"{Range} -> {string.Join(',', SubIntervals.Select(s=> s.Range.ToString()))}";
        }
    }

    private static HashSet<int> CreateSettingsForAllExpanded(IEnumerable<(int start, int length)> collapsibleRanges,
        int totalLineCount)
    {
        return new HashSet<int>();
    }

    private static HashSet<int> CreateSettingsForAllCollapsed(IEnumerable<(int start, int length)> collapsibleRanges,
        int totalLineCount)
    {
        return new HashSet<int>(collapsibleRanges.Select(c => c.start));
    }

    private static HashSet<int> CreateDefaultSettings(IEnumerable<(int start, int length)> collapsibleRanges, int totalLineCount)
    {
        var ranges = collapsibleRanges.OrderBy(v => v.start).ToList();

        IEnumerable<HierarchicalInterval> CreateHierarchicalIntervals(
            IEnumerable<(int start, int length)> sourceRanges)
        {
            Stack<HierarchicalInterval> traverseStack = new();

            foreach (var range in sourceRanges)
            {
                if (!traverseStack.TryPeek(out var currentInterval))
                {
                    currentInterval = new HierarchicalInterval { Range = range };
                    traverseStack.Push(currentInterval);
                    continue;
                }

                while (true)
                {
                    if (traverseStack.TryPeek(out var newCurrentInterval))
                    {
                        currentInterval = newCurrentInterval;
                        if (range.start < currentInterval.Range.start + currentInterval.Range.length)
                        {
                            var newSubInterval = new HierarchicalInterval { Range = range };
                            currentInterval.SubIntervals.Add(newSubInterval);
                            traverseStack.Push(newSubInterval);
                            break;
                        }
                    }
                    else
                    {
                        yield return currentInterval;
                        traverseStack.Push(new HierarchicalInterval { Range = range });
                        break;
                    }

                    _ = traverseStack.Pop();
                }
            }

            HierarchicalInterval? last = null;
            while (traverseStack.Count > 0)
                last = traverseStack.Pop();

            if (last != null)
                yield return last;
        }

        var hierarchicalIntervals = CreateHierarchicalIntervals(ranges);

        var expandedIntervals = new HashSet<int>();

        Queue<HierarchicalInterval> traverseQueue = new(hierarchicalIntervals);
        var minLength = totalLineCount - traverseQueue.Sum(h => h.Range.length - 1);
        var rest = DefaultMaxVisibleLines - minLength;

        while (rest > 0 && traverseQueue.TryDequeue(out var interval))
        {
            var intervalMinLength = interval.MinLength;
            if (rest >= intervalMinLength)
            {
                rest -= intervalMinLength;
                expandedIntervals.Add(interval.Range.start);
                foreach (var subInterval in interval.SubIntervals)
                {
                    traverseQueue.Enqueue(subInterval);
                }
            }
        }

        var collapsedIntervals = ranges.Select(r => r.start).Except(expandedIntervals);
        return new HashSet<int>(collapsedIntervals);
    }
    
    private readonly Dictionary<int, HashSet<int>?> _collapsedLineIndicesByUniqueId = new();
}