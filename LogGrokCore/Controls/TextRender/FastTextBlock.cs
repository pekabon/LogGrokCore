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
using LogGrokCore.Data;

namespace LogGrokCore.Controls.TextRender
{
    public class FastTextBlock : Control
    {
        private PooledList<GlyphLine>? _textLines;
        private readonly Lazy<GlyphTypeface> _glyphTypeface;

        private static readonly Dictionary<(FontFamily, FontStyle, FontWeight, FontStretch), GlyphTypeface>
            TypefaceCache = new();

        public static DependencyProperty HighlightRegex = DependencyProperty.RegisterAttached(
            nameof(HighlightRegex),
            typeof(Regex),
            typeof(FastTextBlock),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender)
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

        private static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(FastTextBlock),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnTextChanged)
        );

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FastTextBlock fastTextBlock)
            {
                fastTextBlock.UpdateText(e.NewValue as string);
            }
        }

        public static readonly DependencyProperty SelectionBrushProperty =
            TextBoxBase.SelectionBrushProperty.AddOwner(typeof(FastTextBlock),
                new PropertyMetadata(GetDefaultSelectionTextBrush()));


        public static readonly DependencyProperty SelectedTextProperty = DependencyProperty.Register(
            "SelectedText", typeof(string), typeof(FastTextBlock),
            new PropertyMetadata(string.Empty));

        public string SelectedText
        {
            get => (string)GetValue(SelectedTextProperty);
            set => SetValue(SelectedTextProperty, value);
        }

        public Brush? SelectionBrush
        {
            get => (Brush)GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public FastTextBlock()
        {
            _glyphTypeface = new Lazy<GlyphTypeface>(CreateGlyphTypeface);
        }

        static FastTextBlock()
        {
            CommandManager.RegisterClassCommandBinding(typeof(FastTextBlock),
                new CommandBinding(
                    RoutedCommands.CopyToClipboard,
                    (sender, args) =>
                    {
                        var text = ((FastTextBlock)sender).SelectedText;
                        TextCopy.ClipboardService.SetText(text);
                        args.Handled = true;
                    },
                    (sender, args) =>
                    {
                        if (sender is not FastTextBlock selectableTextBlock) return;
                        var haveSelectedText = !string.IsNullOrEmpty(selectableTextBlock.SelectedText);
                        args.CanExecute = haveSelectedText;
                        args.Handled = haveSelectedText;
                    }));
        }

        private Point? _startSelectionPoint;

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
                var glyphLine = textLines[currentLine];

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

        private static int GetLineByVerticalPosition(double verticalPosition, IList<GlyphLine> lines)
        {
            if (verticalPosition < 0)
                return 0;
            var currentVerticalOffset = 0.0;
            for (var i = 0; i < lines.Count; i++)
            {
                var height = lines[i].AdvanceHeight;
                if (verticalPosition >= currentVerticalOffset
                    && verticalPosition < currentVerticalOffset + height)
                {
                    return i;
                }

                currentVerticalOffset += height;
            }

            return lines.Count - 1;
        }

        private static (int lineNumber, int textPosition) GetTextPosition(Point point, IList<GlyphLine> lines)
        {
            var y = GetLineByVerticalPosition(point.Y, lines);
            var x = lines[y].GetNearestTextPosition(point.X);
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

                if (e.ClickCount == 2 && _textLines is {} textLines)
                {
                    _startSelectionPoint = null;
                    var point = e.GetPosition(this);
                    SelectWordUnderPoint(point, textLines);
                }
            }
            
            base.OnMouseDown(e);
        }

        private void SelectWordUnderPoint(Point point, PooledList<GlyphLine> textLines)
        {
            var (lineNumber, position) = GetTextPosition(point, textLines);
            var line = textLines[lineNumber];
            var (start, length) = GetWordSelectionRange(line.Text, position);
            line.SetSelection(start, length);
            InvalidateVisual();
            SelectedText = line.Text.Span.Slice(start, length).ToString();
        }

        private static (int start, int length) GetWordSelectionRange(StringRange lineText, int position)
        {
            var span = lineText.Span;
            var left = position;

            bool IsWordPart(char ch) => ch == '_' || char.IsLetterOrDigit(ch); 

            while (left > 0 && IsWordPart(span[left-1]))
            {
                left--;
            }
            
            var right = position;
            while (right < span.Length - 1 && IsWordPart(span[right+1]))
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
                    SelectedText = string.Join("\r\n",
                        _textLines
                            .Where(line => line.Selection != null)
                            .Select(line =>
                            {
                                var str = line.Text.ToString();
                                var (start, length) = line.Selection ?? (0, 0);
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
            if (_textLines != null )
            {
                foreach (var line in _textLines)
                {
                    line.ResetSelection();
                }

                InvalidateVisual();
            }

            base.OnLostFocus(e);
        }

        private void UpdateText(string? newText)
        {
            if (_textLines != null )
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

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return base.ArrangeOverride(arrangeBounds);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var verticalPosition = 0.0; ;

            if (_textLines == null) return;

            var highlightGeometries =
                GetHighlightGeometries(Text,
                    GetHighlightRegex(this));

            if (highlightGeometries != null)
                drawingContext.DrawGeometry(Brushes.Moccasin,
                    new Pen(Brushes.Moccasin, 0),
                    highlightGeometries);

            var selectionGeometries = GetSelectionGeometries();
            if (selectionGeometries != null)
            {
                var selectionBrush = SelectionBrush;
                if (selectionBrush != null)
                    drawingContext.DrawGeometry(selectionBrush,
                        null,
                        selectionGeometries);
            }

            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            foreach (var textLine in _textLines)
            {
                textLine.Render(
                    new Point(0,
                        Math.Round(verticalPosition * pixelsPerDip, MidpointRounding.ToEven) / pixelsPerDip),
                    Foreground, drawingContext);
                verticalPosition += textLine.AdvanceHeight;
            }
        }

        private Geometry GeometryFromRect(Rect rect) => new RectangleGeometry(
            Rect.Inflate(rect, new Size(2, 2)), 3, 3);

        private Geometry? GetSelectionGeometries()
        {
            if (_textLines == null) return null;

            GeometryGroup? accumulatedGeometry = null; // = new GeometryGroup {FillRule = FillRule.Nonzero};

            var verticalOffset = 0.0;

            foreach (var textLine in _textLines)
            {
                var selection = textLine.Selection;
                if (selection.HasValue)
                {
                    var (start, length) = selection.Value;
                    accumulatedGeometry ??= new GeometryGroup { FillRule = FillRule.Nonzero };

                    var rect = textLine.GetTextBounds(new Point(0, verticalOffset), start, length);
                    var geometry = GeometryFromRect(rect);

                    accumulatedGeometry.Children.Add(geometry);
                }

                verticalOffset += textLine.AdvanceHeight;
            }

            return accumulatedGeometry;
        }

        private double _cachedWidth;
        private string? _cachedText;
        
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
            
            var height = 0.0;
            var width = 0.0;

            foreach (var textLine in _textLines)
            {
                height += textLine.AdvanceHeight;
                width = Math.Max(width, textLine.Size.Width);
            }

            return new Size(width, height);
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

        private Geometry? GetHighlightGeometries(string? text, Regex? regex)
        {
            _cachedGetDrawingGeometries ??=
                Cached.Of<(string? text, Regex? regex), Geometry?>(
                    value => GetDrawingGeometriesUncached(
                        _textLines, value.regex));
            return _cachedGetDrawingGeometries((text, regex));
        }

        private Geometry? GetDrawingGeometriesUncached(PooledList<GlyphLine>? textLines, Regex? regex)
        {
            if (textLines == null || regex == null || textLines.Count == 0)
            {
                return null;
            }

            var accumulatedGeometry = new GeometryGroup { FillRule = FillRule.Nonzero };

            var y = 0.0;
            foreach (var textLine in textLines)
            {
                var line = textLine.Text.ToString();
                var matches = regex.Matches(line).ToList();
                foreach (var match in matches.Where(m => m.Length > 0))
                {
                    var rect = textLine.GetTextBounds(new Point(0, y), match.Index, match.Length);
                    var geometry = GeometryFromRect(rect);
                    accumulatedGeometry.Children.Add(geometry);
                }

                y += textLine.Size.Height;
            }

            return accumulatedGeometry;
        }

        private static Brush GetDefaultSelectionTextBrush()
        {
            SolidColorBrush solidColorBrush = new(SystemColors.HighlightColor);
            solidColorBrush.Freeze();
            return solidColorBrush;
        }

        private Func<(string? text, Regex? regex), Geometry?>? _cachedGetDrawingGeometries;
    }
}