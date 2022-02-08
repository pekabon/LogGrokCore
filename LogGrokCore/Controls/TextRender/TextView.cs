using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using LogGrokCore.Data;

namespace LogGrokCore.Controls.TextRender;

public class TextView : Control, IClippingRectChangesAware
{
    private class OutlineData
    {
        public OutlineData(int lineCount, 
            (int start, int length)[] collapsibleRegions,
            Func<HashSet<int>?> collapsedLineIndicesGetter)
        {
            CollapsibleRegionsMachine = new CollapsibleRegionsMachine(lineCount, collapsibleRegions, collapsedLineIndicesGetter);
            
            foreach (var (start, length) in collapsibleRegions)
            {
                foreach (var index in Enumerable.Range(start, length))
                {
                    CollapsibleLineIndices.Add(index);
                }
            }
        }
        
        public readonly CollapsibleRegionsMachine CollapsibleRegionsMachine;
        public readonly HashSet<int> CollapsibleLineIndices = new();
        public readonly Dictionary<int, Rect> ChildrenRectangles = new();
        public Dictionary<int, OutlineExpander> ChildrenByPosition = new();
    }

    private OutlineData? _outlineData;

    private PooledList<GlyphLine>? _textLines;
    private readonly Lazy<GlyphTypeface> _glyphTypeface;
    private const double ExpanderSize = 8;
    public const double ExpanderMargin = 12;

    private UIElementCollection? _children;

    private static readonly Brush OutlineBrush = Brushes.Gray;
    private readonly TextControl _textControl;
    private GuideLinesControl? _guideLinesControl;
    
    private double _cachedWidth;
    private TextModel? _cachedTextModel;
    private bool _isCollapsibleStateDirty;
    private TextViewSharedFoldingState? _sharedFoldingState;
    private FrameworkElement? _clippingRectProvider;

    private UIElementCollection Children
    {
        get
        {
            if (_children != null) return _children;
            _children = new UIElementCollection(this, this) { _textControl };
            return _children;
        }
    }

    private static readonly Dictionary<(FontFamily, FontStyle, FontWeight, FontStretch), GlyphTypeface>
        TypefaceCache = new();

    #region HiglightRegex property

    public static DependencyProperty HighlightRegex = DependencyProperty.RegisterAttached(
        nameof(HighlightRegex),
        typeof(Regex),
        typeof(TextView),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender,
            static (t, _) => (t as TextView)?._textControl.InvalidateVisual())
    );

    public static Regex? GetHighlightRegex(DependencyObject? d)
    {
        if (d == null) throw new NullReferenceException(nameof(d));
        return d.GetValue(HighlightRegex) as Regex;
    }

    public static void SetHighlightRegex(DependencyObject? d, Regex value)
    {
        if (d == null) throw new NullReferenceException(nameof(d));
        d.SetValue(HighlightRegex, value);
    }

    #endregion

    #region TextModel property

    public static readonly DependencyProperty TextModelProperty = DependencyProperty.Register(
        "TextModel", typeof(TextModel), typeof(TextView), 
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
            OnTextModelChanged));

    public TextModel? TextModel
    {
        get => (TextModel?)GetValue(TextModelProperty);
        set => SetValue(TextModelProperty, value);
    }

    private HashSet<int>? GetSharedFoldingState()
    {
        if (SharedFoldingState is not { } sharedState || TextModel is not { } textModel) 
            return null;
        
        if (sharedState[textModel.UniqueId] is { } settings) 
            return settings;
        
        if (textModel is not
            {
                CollapsibleRanges: {} collapsibleRanges,
                Count: var totalLineCount
            })
            return null;

        settings = sharedState.GetDefaultSettings(collapsibleRanges, totalLineCount);
        sharedState[textModel.UniqueId] = settings;
        return settings;
    }
    
    private static void OnTextModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextView textView)
            return;
        
        textView.ResetText();

        textView.SetCollapsibleRanges(textView.TextModel?.CollapsibleRanges);
    }

    void SetCollapsibleRanges(List<(int start, int length)>? collapsibleRanges)
    {
        if (collapsibleRanges == null)
        {
            _outlineData = null;
            FoldingManager = null;
            SharedFoldingState?.Unregister(this);
            return;
        }

        var count = TextModel?.Count ?? 0;
        var collapsibleRangesArray = collapsibleRanges.ToArray();
        _outlineData = new OutlineData(
            count, 
            collapsibleRangesArray,
            GetSharedFoldingState);
        
        _outlineData.CollapsibleRegionsMachine.Changed += () =>
        {
            _isCollapsibleStateDirty = true;
            InvalidateMeasure();
            InvalidateVisual();
        };

        if (SharedFoldingState is not { } sharedFoldingState)
            return;

        SharedFoldingState.Register(this);
        FoldingManager = new FoldingManager(
            _outlineData.CollapsibleRegionsMachine,
            sharedFoldingState,
            () => sharedFoldingState.GetDefaultFoldingSettings(collapsibleRangesArray, count));
    }

    #endregion

    #region SelectionBrush property

    public static readonly DependencyProperty SelectionBrushProperty =
        TextBoxBase.SelectionBrushProperty.AddOwner(typeof(TextView),
            new PropertyMetadata(GetDefaultSelectionTextBrush()));

    public Brush? SelectionBrush
    {
        get => (Brush)GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    #endregion

    #region SelectedText property

    public static readonly DependencyProperty SelectedTextProperty = DependencyProperty.Register(
        nameof(SelectedText), typeof(string), typeof(TextView),
        new PropertyMetadata(string.Empty));

    public string SelectedText
    {
        get => (string)GetValue(SelectedTextProperty);
        set => SetValue(SelectedTextProperty, value);
    }

    #endregion

    #region TransientSettings property
    
    public static readonly DependencyProperty SharedFoldingStateProperty = DependencyProperty.RegisterAttached(
        "SharedFoldingState", typeof(TextViewSharedFoldingState), typeof(TextView), 
            new FrameworkPropertyMetadata(default(TextViewSharedFoldingState), 
                FrameworkPropertyMetadataOptions.Inherits));

    public static void SetSharedFoldingState(DependencyObject element, TextViewSharedFoldingState value)
    {
        element.SetValue(SharedFoldingStateProperty, value);
    }

    public static TextViewSharedFoldingState? GetSharedFoldingState(DependencyObject element)
    {
        return (TextViewSharedFoldingState)element.GetValue(SharedFoldingStateProperty);
    }

    private TextViewSharedFoldingState? SharedFoldingState
    {
        get
        {
            if (_sharedFoldingState == null)
            {
                _sharedFoldingState = GetSharedFoldingState(this);
            }

            return _sharedFoldingState;
        }
    }
    
    #endregion

    public static readonly DependencyProperty FoldingManagerProperty = DependencyProperty.Register(
        "FoldingManager", typeof(FoldingManager), typeof(TextView), 
        new FrameworkPropertyMetadata(default(FoldingManager)));

    public FoldingManager? FoldingManager
    {
        get => (FoldingManager?)GetValue(FoldingManagerProperty);
        set => SetValue(FoldingManagerProperty, value);
    }
    
    public TextView()
    {
        _textControl = new TextControl(this);
        _glyphTypeface = new Lazy<GlyphTypeface>(CreateGlyphTypeface);
    }

    static TextView()
    {
        ForegroundProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender,
            static (d, _) => (d as TextView)?._textControl.InvalidateVisual()));
        
        CommandManager.RegisterClassCommandBinding(typeof(TextView),
            new CommandBinding(
                RoutedCommands.CopyToClipboard,
                (sender, args) =>
                {
                    var text = ((TextView)sender).SelectedText;
                    TextCopy.ClipboardService.SetText(text);
                    args.Handled = true;
                },
                (sender, args) =>
                {
                    if (sender is not TextView selectableTextBlock) return;
                    var haveSelectedText = !string.IsNullOrEmpty(selectableTextBlock.SelectedText);
                    args.CanExecute = haveSelectedText;
                    args.Handled = haveSelectedText;
                }));
    }

    protected override int VisualChildrenCount => _children?.Count ?? 0;

    protected override Visual GetVisualChild(int index)
    {
        if (_children == null)
            throw new InvalidOperationException();
        return _children[index];
    }

    private OutlineExpander AddAndMeasureChild(Outline outline, int index, OutlineData outlineData)
    {
        var expanderState = outline switch
        {
            Collapsed => OutlineExpanderState.Collapsed,
            ExpandedUpper => OutlineExpanderState.ExpandedUpper,
            ExpandedLower => OutlineExpanderState.ExpandedLower,
            _ => throw new NotSupportedException()
        };

        var newChildCreated = false;
        if (!outlineData.ChildrenByPosition.TryGetValue(index, out var expander))
        {
            expander = new OutlineExpander() {Foreground = OutlineBrush};
            newChildCreated = true;
        }

        expander.State = expanderState;
        expander.Expandable = outline as Expandable;

        if (newChildCreated)
            Children.Add(expander);
        
        expander.Measure(new Size(ExpanderSize, double.PositiveInfinity));
        outlineData.ChildrenByPosition[index] = expander;
        return expander;
    }

    private Rect? GetClippingRect()
    {
        _clippingRectProvider ??= ClippingRectProviderBehavior.GetClippingRectProvider(this);
        if (_clippingRectProvider is not {} clippingRectProvider)
            return null;
        
        return ClippingRectProviderBehavior.GetClippingRect(clippingRectProvider, this);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var text = TextModel;
        if (text == null) return new Size(0, 0);

        if (_textLines == null || _cachedTextModel != text || _cachedWidth < constraint.Width || _isCollapsibleStateDirty)
        { 
            ResetText();
            _textLines = CreateTextLines(text, constraint.Width);
            _cachedWidth = constraint.Width;
            _cachedTextModel = text;
            _isCollapsibleStateDirty = false;

            if (text.CollapsibleRanges == null && _guideLinesControl != null)
            {
                Children.Remove(_guideLinesControl);
                _guideLinesControl = null;
            }

            if (text.CollapsibleRanges != null && _guideLinesControl == null)
            {
                _guideLinesControl = new GuideLinesControl();
                Children.Add(_guideLinesControl);
            }
        }
        
        var outlineData = _outlineData;
        var visibleLineIndices = 
            outlineData == null || outlineData.CollapsibleRegionsMachine.LineCount == _textLines.Count
            ? Enumerable.Range(0, _textLines.Count)
            : outlineData.CollapsibleRegionsMachine.Select((oi) => oi.index).ToList();
        
        var visibleLines = visibleLineIndices.Select(idx => (
            textLine: _textLines[idx],
            isCollapsible: outlineData?.CollapsibleLineIndices.Contains(idx) ?? false));

        _textControl.TextLines = visibleLines.ToList();
        _textControl.Measure(constraint);

        var measuredSize = new Size(_textControl.DesiredSize.Width, _textControl.DesiredSize.Height);
        return measuredSize;
    }
    
    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        if (_textLines == null) return arrangeBounds;
        
        var textControlRect =
            new Rect(0, 0, _textControl.DesiredSize.Width, _textControl.DesiredSize.Height);
        _textControl.Arrange(textControlRect);

        RearrangeOutlineChildren(GetClippingRect(), arrangeBounds);
        return arrangeBounds;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var background = Background ?? Brushes.Transparent;
        drawingContext.DrawRectangle(background, new Pen(background, 0), new Rect(0, 0, ActualWidth, ActualHeight));
        base.OnRender(drawingContext);
    }

    private void RearrangeOutlineChildren(Rect? clippingRect, Size arrangeBounds)
    {
        if (_outlineData is not { } outlineData)
        {
            var children = Children;
            for (var i = children.Count - 1; i >= 0; i--)
            {
                if (children[i] is OutlineExpander)
                    children.RemoveAt(i);
            }
            return;
        }

        var (newChildren, newChildrenByPosition) =
            UpdateChildren(clippingRect, _outlineData);

        outlineData.ChildrenByPosition = newChildrenByPosition;

        var toDelete = Children.OfType<OutlineExpander>().Except(newChildren).ToList();
        var toAdd = newChildren.Except(Children.OfType<OutlineExpander>()).ToList();
        foreach (var outlineExpander in toDelete)
        {
            _children?.Remove(outlineExpander);
        }

        foreach (var outlineExpander in toAdd)
        {
            _children?.Add(outlineExpander);
        }

        for (var i = 0; i < outlineData.ChildrenByPosition.Count; i++)
        {
            var (index, expander) = outlineData.ChildrenByPosition.ElementAt(i);
            var rect = outlineData.ChildrenRectangles[index];
            expander.Arrange(rect);
        }

        var childrenRectangles = _outlineData?.ChildrenRectangles;

        if (childrenRectangles is not { } || _guideLinesControl is not { } guideLinesControl)
        {
            return;
        }

        var sortedRectangles =
            childrenRectangles.OrderBy(kv => kv.Key)
                .Select(kv => kv.Value).ToList();

        guideLinesControl.Arrange(new 
            Rect(0, 0, ExpanderSize, arrangeBounds.Height));

        var newLines = new List<(double, double)>();
        for (var i = 1; i < sortedRectangles.Count; i++)
        {
            var prev = sortedRectangles[i - 1];
            var current = sortedRectangles[i];
            newLines.Add((prev.Bottom, current.Top));
        }

        if (!(guideLinesControl.Lines?.SequenceEqual(newLines) ?? false))
        {
            guideLinesControl.Lines = newLines;
        }
    }

    private (HashSet<OutlineExpander> newChildren, 
        Dictionary<int, OutlineExpander> newChildrenByPosition) UpdateChildren(Rect? clippingRect, OutlineData outlineData)
    {
        if (_textLines == null)
            throw new InvalidOperationException();
        
        double verticalPosition = 0;
        var pixelsPerDip = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;

        outlineData.ChildrenRectangles.Clear();
        HashSet<OutlineExpander> newChildren = new(); 
        Dictionary<int, OutlineExpander> newChildrenByPosition = new();
        
        for (var i = 0; i < outlineData.CollapsibleRegionsMachine.LineCount; i++)
        {
            var (outline, index) = outlineData.CollapsibleRegionsMachine[i];
            var textLine = _textLines[index];
            var yCenter = Math.Round((verticalPosition + textLine.Size.Height / 2) * pixelsPerDip,
                MidpointRounding.ToEven) / pixelsPerDip;

            if (outline is not None)
            {
                var ySize = ExpanderSize;
                var xSize = ExpanderSize;
                var rect = new Rect(0, yCenter - ySize / 2, xSize, ySize);

                if (clippingRect == null ||
                    clippingRect is {} clip 
                        && (rect.IntersectsWith(clip) || clip.Contains(rect) || rect.Contains(clip)))
                {
                    var newChild = AddAndMeasureChild(outline, index, outlineData);
                    newChildren.Add(newChild);
                    newChildrenByPosition[index] = newChild;
                    var desiredSize = newChild.DesiredSize;
                    xSize = desiredSize.Width;
                    ySize = desiredSize.Height;
                }
                
                rect = new Rect(0, 
                    yCenter - ySize / 2, 
                    xSize, ySize);
                outlineData.ChildrenRectangles[index] = rect;
            }

            verticalPosition += textLine.AdvanceHeight;
        }

        return (newChildren, newChildrenByPosition);
    }
    
    private void ResetText()
    {
        if (_textLines != null)
        {
            foreach (var textLine in _textLines)
            {
                textLine.Dispose();
            }

            _textLines.Dispose();
        }

        _textLines = null;
    }

    private PooledList<GlyphLine> CreateTextLines(TextModel newText, double constraintWidth)
    {
        var list = new PooledList<GlyphLine>(16);
        var glyphTypeFace = _glyphTypeface.Value;
        var pixelsPerDip = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var lineIndex = 0;
        var textModel = TextModel;
        
        StringRange ApplyCollapsedPostfix(StringRange stringRange)
        {
            if (!(_outlineData?.CollapsibleRegionsMachine.IsCollapsed(lineIndex) ?? false)) 
                return stringRange;
            
            if (textModel?.GetCollapsedTextSubstitution(lineIndex) is { IsEmpty: false } substitution)
            {
                return substitution;
            }
            return StringRange.FromString(stringRange.ToString().TrimEnd().TrimEnd('{') + "{...}");
        }

        foreach (var stringRange in newText)
        {
            var lineWithPostfix = ApplyCollapsedPostfix(stringRange);
            list.Add(new GlyphLine(lineWithPostfix, glyphTypeFace, FontSize, pixelsPerDip, constraintWidth));
            lineIndex++;
        }

        return list;
    }

    private GlyphTypeface CreateGlyphTypeface()
    {
        var key = (FontFamily, FontStyle, FontWeight, FontStretch);
        if (TypefaceCache.TryGetValue(key, out var glyphTypeface))
            return glyphTypeface;

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            throw new NotSupportedException();
        TypefaceCache[key] = glyphTypeface;
        return glyphTypeface;
    }

    private static Brush GetDefaultSelectionTextBrush()
    {
        SolidColorBrush solidColorBrush = new(SystemColors.HighlightColor);
        solidColorBrush.Freeze();
        return solidColorBrush;
    }

    public void OnChildRectChanged(Rect rect)
    {
        if (_outlineData == null)
            return;
        
        InvalidateArrange();
    }

    public void ResetFoldingToDefault()
    {
        if (TextModel is not
            {
                CollapsibleRanges: {} collapsibleRanges,
                Count: var totalLineCount
            } ||
            SharedFoldingState is not {} sharedFoldingState)
            return;
        var defaultSettings = sharedFoldingState.GetDefaultSettings(collapsibleRanges, totalLineCount);
        _outlineData?.CollapsibleRegionsMachine.UpdateCollapsedLines(defaultSettings);
    }

    public override string ToString()
    {
        return TextModel?.ToString() ?? "<null>";
    }
}