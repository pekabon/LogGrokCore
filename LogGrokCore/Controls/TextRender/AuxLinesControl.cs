using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VirtualizingStackPanel = LogGrokCore.Controls.ListControls.VirtualizingStackPanel.VirtualizingStackPanel;

namespace LogGrokCore.Controls.TextRender;

public class AuxLinesControl : Control
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

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (Lines == null) 
            return;
        
        var clippingRect = GetClippingRect();
        Rect? inflated = null;
        if (clippingRect != null)
        {
            var temp = clippingRect.Value;
            temp.Inflate(new Size(0, clippingRect.Value.Height));
            inflated = temp;
        }

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
    
    Rect? GetClippingRect()
    {
        var ss = VirtualizingStackPanel.GetClippingRect(this);
        if (ss is not { } r) return null;
        var (rect, _) = r;

        var result = new Rect(PointFromScreen(rect.TopLeft), PointFromScreen(rect.BottomRight));
        return result;
    }

    public void Update()
    {
        var clippingRect = GetClippingRect();

        var visibleRenderedLines =
            EnumerateLinesInRect(_renderedLines, clippingRect).ToHashSet();

        if (visibleRenderedLines.SetEquals(EnumerateLinesInRect(Lines, clippingRect)))
            return;

        InvalidateVisual();
    }
}