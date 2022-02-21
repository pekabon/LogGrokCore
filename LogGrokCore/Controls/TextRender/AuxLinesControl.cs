using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogGrokCore.Controls.TextRender;

public class AuxLinesControl : Control, IClippingRectChangesAware
{
    private static readonly Brush OutlineBrush = Brushes.Gray;    
    private double _width;
    private readonly HashSet<(double, double)> _renderedLines = new();

    public static readonly DependencyProperty LinesProperty = DependencyProperty.Register(
        nameof(Lines), typeof(List<(double, double)>), typeof(AuxLinesControl), 
        new FrameworkPropertyMetadata(default(List<(double, double)>), FrameworkPropertyMetadataOptions.AffectsRender));

    public List<(double, double)>? Lines
    {
        get => (List<(double, double)>?)GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        _width = arrangeBounds.Width;
        return base.ArrangeOverride(arrangeBounds);
    }

    private IEnumerable<(double, double)> EnumerateLinesInRect(IEnumerable<(double, double)>? lines, Rect? rect)
    {
        if (lines == null)
            yield break;
        
        foreach (var (y1, y2) in lines)
        {
            if (rect is not { } r ||
                (y1 <= r.Bottom && y1 >= r.Top) || (y2 <= r.Bottom && y2 >= r.Top))
                yield return (y1, y2);
        }
    }

    private static Rect? InflateHeightTwice(Rect? source)
    {
        if (source == null)
            return null;
        
        var inflated = source.Value;
        inflated.Inflate(new Size(0, source.Value.Height));
        return inflated;
    }
    
    protected override void OnRender(DrawingContext drawingContext)
    {
        if (Lines == null) 
            return;
        
        var clippingRect = GetClippingRect();
        Rect? inflated = InflateHeightTwice(clippingRect);

        var x = _width / 2;
        var outlinePen = new Pen(OutlineBrush, 0.5);

        var lines = inflated != null ? EnumerateLinesInRect(Lines, inflated.Value) : Lines;
        _renderedLines.Clear();
        foreach (var (y1, y2) in lines)
        {
            drawingContext.DrawLine(outlinePen, new Point(x, y1), new Point(x, y2));
            _renderedLines.Add((y1, y2));
        }
    }
    
    private FrameworkElement? _clippingRectProvider;

    private Rect? GetClippingRect()
    {
        _clippingRectProvider ??= ClippingRectProviderBehavior.GetClippingRectProvider(this);
        if (_clippingRectProvider is not {} clippingRectProvider)
            return null;
        
        return ClippingRectProviderBehavior.GetClippingRect(clippingRectProvider, this);
    }

    private void Update(Rect? clippingRect)
    {
        var visibleRenderedLines =
            EnumerateLinesInRect(_renderedLines, clippingRect).ToHashSet();

        if (visibleRenderedLines.SetEquals(EnumerateLinesInRect(Lines, clippingRect)))
            return;

        InvalidateVisual();
    }

    public void OnChildRectChanged(Rect rect)
    {
        Update(rect);
    }
}