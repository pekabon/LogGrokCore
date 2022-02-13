using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LogGrokCore.Data;

namespace LogGrokCore.Controls.TextRender;

public class TextControl : Control
{
    private IList<(GlyphLine glyphLine, bool isCollapsible)>? _textLines;

    private readonly TextView _textView;
    private Point? _startSelectionPoint;

    public TextControl(TextView textView)
    {
        _textView = textView;
    }

    public void SetTextLines(IList<(GlyphLine textLine, bool isCollapsible)>? textLines)
    {
        if (_textLines == null && textLines == null)
        {
            return;
        }

        _textLines = textLines;
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (_textLines == null) return new Size(0, 0);
        var height = 0.0;
        var width = 0.0;
        foreach (var textLine in _textLines)
        {
            height += textLine.glyphLine.AdvanceHeight;
            width = Math.Max(width, textLine.glyphLine.Size.Width
                                    + GetHorizontalOffset(textLine));
        }

        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_textLines == null)
            return;

        var highlightGeometries = GetHighlightGeometries(_textLines, TextView.GetHighlightRegex(_textView));

        if (highlightGeometries != null)
        {
            drawingContext.DrawGeometry(Brushes.Moccasin,
                new Pen(Brushes.Moccasin, 0),
                highlightGeometries);
        }

        var selectionGeometries = GetSelectionGeometries();
        if (selectionGeometries != null)
        {
            var selectionBrush = _textView.SelectionBrush;
            if (selectionBrush != null)
                drawingContext.DrawGeometry(selectionBrush,
                    null,
                    selectionGeometries);
        }

        var verticalPosition = 0.0;
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var foreground = Foreground;
        foreach (var textLine in _textLines)
        {
            textLine.glyphLine.Render(
                new Point(GetHorizontalOffset(textLine),
                    Math.Round(verticalPosition * pixelsPerDip, MidpointRounding.ToEven) / pixelsPerDip),
                foreground, drawingContext);
            verticalPosition += textLine.glyphLine.AdvanceHeight;
        }
    }

    private static double GetHorizontalOffset(bool isCollapsible)
    {
        return isCollapsible ? TextView.ExpanderMargin : 0.0;
    }

    private static double GetHorizontalOffset((GlyphLine glyphLine, bool isCollapsible) textLine)
    {
        return GetHorizontalOffset(textLine.isCollapsible);
    }

    private Func<(IList<(GlyphLine glyphLine, bool isCollapsible)>? text, Regex? regex), Geometry?>?
        _cachedGetDrawingGeometries;

    private Geometry? GetHighlightGeometries(IList<(GlyphLine glyphLine, bool isCollapsible)>? text, Regex? regex)
    {
        _cachedGetDrawingGeometries ??=
            Cached.Of<(IList<(GlyphLine glyphLine, bool isCollapsible)>?, Regex? regex), Geometry?>(
                value => GetDrawingGeometriesUncached(
                    _textLines, value.regex));
        return _cachedGetDrawingGeometries((text, regex));
    }

    private Geometry? GetDrawingGeometriesUncached(IList<(GlyphLine glyphLine, bool isCollapsible)>? textLines,
        Regex? regex)
    {
        if (textLines == null || regex == null || textLines.Count == 0)
        {
            return null;
        }

        var accumulatedGeometry = new GeometryGroup { FillRule = FillRule.Nonzero };

        var y = 0.0;
        foreach (var textLine in textLines)
        {
            var glyphLine = textLine.glyphLine;
            var line = glyphLine.Text.ToString();
            var matches = regex.Matches(line).ToList();
            foreach (var match in matches.Where(m => m.Length > 0))
            {
                var startPoint = GetHorizontalOffset(textLine);
                var rect = glyphLine.GetTextBounds(new Point(startPoint, y), match.Index, match.Length);
                var geometry = GeometryFromRect(rect);

                accumulatedGeometry.Children.Add(geometry);
            }

            y += glyphLine.Size.Height;
        }

        return accumulatedGeometry;
    }

    private Geometry GeometryFromRect(Rect rect) => new RectangleGeometry(
        Rect.Inflate(rect, new Size(2, 2)), 3, 3);

    private Geometry? GetSelectionGeometries()
    {
        if (_textLines == null) return null;

        GeometryGroup? accumulatedGeometry = null; // = new GeometryGroup {FillRule = FillRule.Nonzero};

        var verticalOffset = 0.0;

        foreach (var (glyphLine, isCollapsible) in _textLines)
        {
            var selection = glyphLine.Selection;
            if (selection.HasValue)
            {
                var (start, length) = selection.Value;
                accumulatedGeometry ??= new GeometryGroup { FillRule = FillRule.Nonzero };

                var rect = glyphLine.GetTextBounds(new Point(GetHorizontalOffset(isCollapsible), verticalOffset), start,
                    length);
                var geometry = GeometryFromRect(rect);

                accumulatedGeometry.Children.Add(geometry);
            }

            verticalOffset += glyphLine.AdvanceHeight;
        }

        return accumulatedGeometry;
    }

    #region Selection support

    private void UpdateSelection(Point startSelectionPoint, Point endSelectionPoint)
    {
        if (_textLines == null ||
            _textLines.Count == 0)
        {
            return;
        }

        var textLines = _textLines;

        var textPosition1 = GetTextPosition(startSelectionPoint, textLines);
        var textPosition2 = GetTextPosition(endSelectionPoint, textLines);
        var (upperPosition, lowerPosition) =
            textPosition1.lineNumber < textPosition2.lineNumber
                ? (textPosition1, textPosition2)
                : (textPosition2, textPosition1);

        var selectionChanged = false;
        for (var currentLine = 0; currentLine < textLines.Count; currentLine++)
        {
            var glyphLine = textLines[currentLine].glyphLine;

            if (currentLine < upperPosition.lineNumber || currentLine > lowerPosition.lineNumber)
            {
                selectionChanged |= glyphLine.ResetSelection();
                continue;
            }

            var leftPosition = currentLine == upperPosition.lineNumber
                ? upperPosition.textPosition
                : 0;

            var rightPosition = currentLine == lowerPosition.lineNumber
                ? lowerPosition.textPosition
                : glyphLine.Text.Length;

            if (leftPosition == rightPosition)
                selectionChanged |= glyphLine.ResetSelection();
            else
                selectionChanged |= glyphLine.SetSelection(Math.Min(leftPosition, rightPosition),
                    Math.Abs(rightPosition - leftPosition));
        }

        if (!selectionChanged) return;

        InvalidateVisual();
    }

    private static int GetLineByVerticalPosition(double verticalPosition,
        IList<(GlyphLine glyphLine, bool isCollapsible)> lines)
    {
        if (verticalPosition < 0)
            return 0;
        var currentVerticalOffset = 0.0;
        for (var i = 0; i < lines.Count; i++)
        {
            var height = lines[i].glyphLine.AdvanceHeight;
            if (verticalPosition >= currentVerticalOffset
                && verticalPosition < currentVerticalOffset + height)
            {
                return i;
            }

            currentVerticalOffset += height;
        }

        return lines.Count - 1;
    }

    private static (int lineNumber, int textPosition) GetTextPosition(Point point,
        IList<(GlyphLine glyphLine, bool isCollapsible)> lines)
    {
        var y = GetLineByVerticalPosition(point.Y, lines);
        var (glyphLine, isCollapsible) = lines[y];
        var x = glyphLine.GetNearestTextPosition(point.X - GetHorizontalOffset(isCollapsible));
        return (y, x);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        Focus();
        if (e.ChangedButton == MouseButton.Left)
        {
            if (e.ClickCount == 1)
            {
                _startSelectionPoint = e.GetPosition(this);
            }

            if (e.ClickCount == 2 && _textLines is { } textLines)
            {
                _startSelectionPoint = null;
                var point = e.GetPosition(this);
                SelectWordUnderPoint(point, textLines);
            }
        }

        base.OnMouseDown(e);
    }

    private void SelectWordUnderPoint(Point point, IList<(GlyphLine glyphLine, bool isCollapsible)> textLines)
    {
        var (lineNumber, position) = GetTextPosition(point, textLines);
        var (line, _) = textLines[lineNumber];
        var (start, length) = GetWordSelectionRange(line.Text, position);
        line.SetSelection(start, length);
        InvalidateVisual();
        _textView.SelectedText = line.Text.Span.Slice(start, length).ToString();
    }

    private static (int start, int length) GetWordSelectionRange(StringRange lineText, int position)
    {
        var span = lineText.Span;
        var left = position;

        bool IsWordPart(char ch) => ch == '_' || char.IsLetterOrDigit(ch);

        while (left > 0 && IsWordPart(span[left - 1]))
        {
            left--;
        }

        var right = position;
        while (right < span.Length - 1 && IsWordPart(span[right + 1]))
        {
            right++;
        }

        return (left, right - left + 1);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        var startSelectionPoint = _startSelectionPoint;
        if (startSelectionPoint.HasValue)
        {
            _startSelectionPoint = null;
            ReleaseMouseCapture();
            UpdateCursor();
            var endPosition = e.GetPosition(this);
            UpdateSelection(startSelectionPoint.Value, endPosition);
            if (_textLines != null)
                _textView.SelectedText = string.Join("\r\n",
                    _textLines
                        .Where(line => line.glyphLine.Selection != null)
                        .Select(line =>
                        {
                            var str = line.glyphLine.Text.ToString();
                            var (start, length) = line.glyphLine.Selection ?? (0, 0);
                            return str.Substring(start, length);
                        }));
        }

        base.OnMouseUp(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_startSelectionPoint.HasValue && _textLines != null && _textLines.Count != 0)
        {
            if (!IsMouseCaptured)
            {
                CaptureMouse();
            }

            if (!IsFocused)
            {
                Focus();
            }

            var endSelectionPoint = e.GetPosition(this);
            UpdateSelection(_startSelectionPoint.Value, endSelectionPoint);
        }

        UpdateCursor();
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (!IsMouseCaptured)
        {
            _startSelectionPoint = null;
        }

        UpdateCursor();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        UpdateCursor();
        base.OnMouseEnter(e);
    }

    private void UpdateCursor()
    {
        if ((IsFocused && IsMouseOver) || IsMouseCaptured)
            Mouse.OverrideCursor = Cursors.IBeam;
        else if (Mouse.OverrideCursor != null)
            Mouse.OverrideCursor = null;
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (_textLines != null)
        {
            foreach (var (line, _) in _textLines)
            {
                line.ResetSelection();
            }

            InvalidateVisual();
        }

        base.OnLostFocus(e);
    }

    #endregion
}