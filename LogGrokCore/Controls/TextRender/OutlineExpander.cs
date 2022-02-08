using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace LogGrokCore.Controls.TextRender;

public enum OutlineExpanderState
{
    Collapsed,
    ExpandedUpper,
    ExpandedLower
}

public class OutlineExpander : ButtonBase
{
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(OutlineExpanderState),
        typeof(OutlineExpander), 
        new PropertyMetadata(default(OutlineExpanderState)));

    public OutlineExpanderState State
    {
        get => (OutlineExpanderState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }
    
    protected override Size MeasureOverride(Size constraint)
    {
        var sz = State switch
        {
            OutlineExpanderState.Collapsed => new Size(1, 1),
            OutlineExpanderState.ExpandedLower => new Size(3, 4),
            OutlineExpanderState.ExpandedUpper => new Size(3, 4),
            _ => throw new ArgumentOutOfRangeException($"Unexpected State Value: {State}.")
        };

        var normalizedByWidth = new Size(constraint.Width, constraint.Width / sz.Width * sz.Height);
        _currentSize = normalizedByWidth.Height <= constraint.Height
            ? normalizedByWidth
            : new Size(constraint.Height / sz.Height * sz.Width, constraint.Height);
        return _currentSize;
    }
    
    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(Brushes.Transparent, new Pen(Brushes.Transparent, 1), new Rect(0, 0, ActualWidth, ActualHeight));
        
        var width = _currentSize.Width;
        var height = _currentSize.Height;

        var xOffset = (ActualWidth - width) / 2;
        var yOffset = (ActualHeight - height) / 2;

        var pen = new Pen(Foreground, 1.0);
        
        void DrawLine(double x1, double y1, double x2, double y2)
        {
            drawingContext.DrawLine(pen, new Point(x1 + xOffset,y1 + yOffset), 
                new Point(x2 + xOffset, y2 + yOffset));
        }
        
        switch (State)
        {
            case OutlineExpanderState.Collapsed:
                DrawLine(0, 0, width, 0);
                DrawLine(width, 0, width, height);
                DrawLine(width, height, 0, height );
                DrawLine(0, height , 0, 0 );
                DrawLine(width/2, height/6, width/2, height * 5/6);
                DrawLine(width/6, height/2, width*5/6, height/2);
                break;
            case OutlineExpanderState.ExpandedUpper:
                DrawLine(0, 0, width, 0);
                DrawLine(width, 0, width, height * 7.5 / 12);
                DrawLine(width, height * 7.5 / 12, width / 2, height);
                DrawLine(width / 2, height, 0, height * 7.5 / 12 );
                DrawLine(0, height * 7.5 / 12, 0, 0);
                DrawLine(width/6, height/2 - height/12, width*5/6, height/2 - height/12);
                break;
            case OutlineExpanderState.ExpandedLower:
                DrawLine(0, height, width, height);
                DrawLine(width, height, width, height * 4.5 / 12);
                DrawLine(width, height * 4.5 / 12, width / 2, 0);
                DrawLine( width / 2,  0, 0, height * 4.5 / 12);
                DrawLine(  0, height * 4.5 / 12, 0, height);
                DrawLine(width/6, height/2 + height/12, width*5/6, height/2 + height/12);
                break;
            default:
                DrawLine(width/6, height/2, width*5/6, height/2);
                break;
        }
    }

    private Size _currentSize;
}