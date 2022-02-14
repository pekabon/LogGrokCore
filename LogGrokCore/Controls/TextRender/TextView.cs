using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using LogGrokCore.Controls.ListControls.VirtualizingStackPanel;
using LogGrokCore.Data;
using VirtualizingStackPanel = LogGrokCore.Controls.ListControls.VirtualizingStackPanel.VirtualizingStackPanel;

namespace LogGrokCore.Controls.TextRender;

public class TextView : Control,IClippingRectChangesAware
{
    private class OutlineData
    {
        public OutlineData(int lineCount, (int start, int length)[] collapsibleRegions)
        {
            CollapsibleRegionsMachine = new CollapsibleRegionsMachine(lineCount, collapsibleRegions);
            
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
    private const double ExpanderSize = 10;
    public const double ExpanderMargin = 20;

    private UIElementCollection? _children;

    private static readonly Brush OutlineBrush = Brushes.Gray;
    private readonly TextControl _textControl;

    private UIElementCollection Children
    {
        get
        {
            _children ??= new UIElementCollection(this, this) { _textControl };
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

    #region Text property

    private static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(TextView),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
            OnTextChanged)
    );

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty CollapsibleTextProperty = DependencyProperty.Register(
        "CollapsibleText", typeof((string text, (int start, int length)[] collapsibleRegions)),
        typeof(TextView), new PropertyMetadata(default((string text, (int start, int length)[] collapsibleRegions))));

    public (string text, (int start, int length)[] collapsibleRegions) CollapsibleText
    {
        get => ((string text, (int start, int length)[] collapsibleRegions))GetValue(CollapsibleTextProperty);
        set => SetValue(CollapsibleTextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextView fastTextBlock)
        {
            fastTextBlock.ResetText();
        }
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

    #region CollpasibleRanges  property

    public static readonly DependencyProperty CollapsibleRangesProperty = DependencyProperty.Register(
        nameof(CollapsibleRanges), typeof(List<(int start, int length)>), typeof(TextView),
        new PropertyMetadata(null, OnCollapsibleRangesChanged));


    private static void OnCollapsibleRangesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextView textView)
            return;

        if (e.NewValue is not List<(int start, int length)> collapsibleRanges)
        {
            textView._outlineData = null;
            textView.InvalidateVisual();
            return;
        }

        textView._outlineData = new OutlineData(textView.Text.Tokenize().Count(), collapsibleRanges.ToArray());
        textView._outlineData.CollapsibleRegionsMachine.Changed += () =>
        {
            textView.InvalidateMeasure();
            textView.InvalidateVisual();
        };
    }

    public List<(int start, int length)>? CollapsibleRanges
    {
        get => GetValue(CollapsibleRangesProperty) as List<(int start, int length)>;
        set => SetValue(CollapsibleRangesProperty, value);
    }

    #endregion

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
        
        expander.Measure(new Size(ExpanderSize, ExpanderSize));
        outlineData.ChildrenByPosition[index] = expander;
        return expander;
    }

    Rect? GetClippingRect()
    {
        
        var ss = VirtualizingStackPanel.GetClippingRect(this);
        if (ss is not { } r) return null;
        var (rect, _) = r;

        var result = new Rect(PointFromScreen(rect.TopLeft), PointFromScreen(rect.BottomRight));
        return result;

    }

    (HashSet<OutlineExpander> newChildren, 
        Dictionary<int, OutlineExpander> newChildrenByPosition) UpdateChildren(Size measureConstraint, OutlineData outlineData)
    {
        if (_textLines == null)
            throw new InvalidOperationException();
        
        double verticalPosition = 0;
        var pixelsPerDip = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var clippingRect = GetClippingRect();
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
                var rect = new Rect(ExpanderMargin / 2 - xSize / 2, yCenter - ySize / 2, xSize, ySize);

                if (clippingRect == null ||
                    clippingRect is {} clip 
                        && (rect.IntersectsWith(clip) || clip.Contains(rect) || rect.Contains(clip)))
                {
                    Debug.WriteLine($"Add and Measure: {index}, ClippingRect = {clippingRect}");
                    var newChild = AddAndMeasureChild(outline, index, outlineData);
                    newChildren.Add(newChild);
                    newChildrenByPosition[index] = newChild;
                    var desiredSize = newChild.DesiredSize;
                    xSize = desiredSize.Width;
                    ySize = desiredSize.Height;
                }
                
                rect = new Rect(ExpanderMargin / 2 - xSize / 2, yCenter - ySize / 2, xSize, ySize);
                outlineData.ChildrenRectangles[index] = rect;
            }

            verticalPosition += textLine.AdvanceHeight;
        }

        return (newChildren, newChildrenByPosition);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var text = Text;
        if (string.IsNullOrEmpty(text)) return new Size(0, 0);

        if (_textLines == null || _cachedText != text || _cachedWidth < constraint.Width)
        {
            _textLines = CreateTextLines(text, constraint.Width);
            _cachedWidth = constraint.Width;
            _cachedText = text;
        }
        
        var outlineData = _outlineData;
        var visibleLineIndices = 
            outlineData == null || outlineData.CollapsibleRegionsMachine.LineCount == _textLines.Count
            ? Enumerable.Range(0, _textLines.Count)
            : outlineData.CollapsibleRegionsMachine.Select((oi) => oi.index).ToList();
        
        var visibleLines = visibleLineIndices.Select(idx => (
            textLine: _textLines[idx],
            isCollapsible: outlineData?.CollapsibleLineIndices.Contains(idx) ?? false));

        _textControl.SetTextLines(visibleLines.ToList());
        _textControl.Measure(constraint);

        return new Size(_textControl.DesiredSize.Width, _textControl.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        if (_textLines == null) return arrangeBounds;
        if (_outlineData is not {} outlineData)
        {
            var children = Children;
            var outlineExpanders = children.OfType<OutlineExpander>();
            foreach (var outlineExpander in outlineExpanders)
            {
                children.Remove(outlineExpander);
            }

            return arrangeBounds;
        }

        var (newChildren, newChildrenByPosition) = 
            UpdateChildren(arrangeBounds, _outlineData);

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

        var textControlRect =
            new Rect(0, 0, _textControl.DesiredSize.Width, _textControl.DesiredSize.Height);
        _textControl.Arrange(textControlRect);

        return arrangeBounds;
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

    private PooledList<GlyphLine> CreateTextLines(string newText, double constraintWidth)
    {
        var list = new PooledList<GlyphLine>(16);
        var glyphTypeFace = _glyphTypeface.Value;
        var pixelsPerDip = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        foreach (var stringRange in newText.Tokenize())
        {
            list.Add(new GlyphLine(stringRange, glyphTypeFace, FontSize, pixelsPerDip, constraintWidth));
        }

        return list;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_textLines == null) return;

        var childrenRectangles = _outlineData?.ChildrenRectangles;

        if (childrenRectangles is not { Count: > 1 })
            return;

        var outlinePen = new Pen(OutlineBrush, 0.5);
        var sortedRectangles =
            childrenRectangles.OrderBy(kv => kv.Key)
                .Select(kv => kv.Value).ToList();
        
        for (var i = 1; i < sortedRectangles.Count; i++)
        {
            var prev = sortedRectangles[i - 1];
            var current = sortedRectangles[i];
            var p1 = new Point((prev.Left + prev.Right) / 2, prev.Bottom);
            var p2 = new Point((current.Left + current.Right) / 2, current.Top);
            drawingContext.DrawLine(outlinePen, p1, p2);
        }
    }

    private double _cachedWidth;
    private string? _cachedText;

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

    public void OnChildRectChanged((Rect, Point)? rect)
    {
        Dispatcher.BeginInvoke(new Action(InvalidateArrange));
    }
}