using System;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogGrokCore.Controls
{
    public class HighlightedTextBlock : Control
    {
        public static DependencyProperty HighlightRegex  = DependencyProperty.RegisterAttached(
            nameof(HighlightRegex),
            typeof(Regex),
            typeof(HighlightedTextBlock),
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

        public static DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(HighlightedTextBlock),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure)
            );

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        protected override Size MeasureOverride(Size _)
        {
            var formattedText = GetFormattedText();
            return MeasureOverrideCore(formattedText);
        }

        private FormattedText? _cachedText;
        private Size _cachedSize;
        private Size MeasureOverrideCore(FormattedText formattedText)
        {
            if (formattedText.Equals(_cachedText)) return _cachedSize;
            _cachedText = formattedText;
            _cachedSize = new Size(formattedText.Width, formattedText.Height);

            return _cachedSize;
        }

        protected override Size ArrangeOverride(Size finalSize) => finalSize;

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Background != null)
                drawingContext.DrawRectangle(Background, 
                    new Pen(Background, 0), 
                    new Rect(0, 0, ActualWidth, ActualHeight));

            var drawingGeometries = 
            GetDrawingGeometries(Text, 
                    GetHighlightRegex(this));
                
            if(drawingGeometries != null)
                drawingContext.DrawGeometry(Brushes.Moccasin, 
                    new Pen(Brushes.Moccasin, 0), 
                    drawingGeometries);

            drawingContext.DrawText(GetFormattedText(), new Point(0, 0));
        }

        private (string?, Regex?) _cachedGetDrawingGeometriesArg = (null, null);
        private Geometry? _cachedGetDrawingGeometriesResult;

        private readonly char[] _newLineSeparators = Environment.NewLine.ToCharArray();
        
        //[CacheLastResult]
        private Geometry? GetDrawingGeometries(string? text, Regex? regex)
        {
            if (string.IsNullOrEmpty(text) || regex == null)
            {
                return null;
            }

            if ((text, regex) == _cachedGetDrawingGeometriesArg)
                return _cachedGetDrawingGeometriesResult;
            
            
            var lines = text.Split(_newLineSeparators);
            
            // dirty performance fix
            // multiple calls to FormattedText.BuildHighlightGeometry can cost > 1 min on very large lines (it seems like it recalculates text line dimensions on every BuildHighlightGeometry call)
            // TODO: Replace FormattedText with custom class

            var accumulatedGeometry = new GeometryGroup {FillRule = FillRule.Nonzero};

            var y = 0.0;
            foreach (var line in lines)
            {
                var matches = regex.Matches(line).Cast<Match>().ToList();
                var ft = 
                    GetFormattedTextUncached(line, FlowDirection, FontFamily, FontStyle, FontWeight, FontStretch, FontSize, Foreground);
                foreach(var match in matches.Where(m => m.Length > 0))
                {
                    var geometry = ft.BuildHighlightGeometry(new Point(0, y), match.Index, match.Length);
                    accumulatedGeometry.Children.Add(geometry);
                }
                y += ft.Height;
            }

            _cachedGetDrawingGeometriesArg = (text, regex);
            _cachedGetDrawingGeometriesResult = accumulatedGeometry;
            return accumulatedGeometry;

        }

        private FormattedText GetFormattedText()
        {
            return GetFormattedText(Text, FlowDirection, FontFamily, FontStyle, FontWeight, FontStretch, FontSize, Foreground);
        }

        readonly struct FormattedTextParams : IEquatable<FormattedTextParams>
        {
            private readonly string _value;
            private readonly FlowDirection _flowDirection;
            private readonly FontFamily _fontFamily;
            private readonly FontStyle _fontStyle;
            private readonly FontWeight _fontWeight;
            private readonly FontStretch _fontStretch;
            private readonly double _fontSize;
            private readonly Brush _foreground;

            public FormattedTextParams(string value, FlowDirection flowDirection, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double fontSize, Brush foreground)
            {
               _value = value;
               _flowDirection = flowDirection;
               _fontFamily = fontFamily;
               _fontStyle = fontStyle;
               _fontWeight = fontWeight;
               _fontStretch = fontStretch;
               _fontSize = fontSize;
               _foreground = foreground;
            }

            public bool Equals(FormattedTextParams other)
            {
                return _value == other._value && _flowDirection == other._flowDirection && 
                       _fontFamily.Equals(other._fontFamily) && 
                       _fontStyle == other._fontStyle && _fontWeight == other._fontWeight &&
                       _fontStretch.Equals(other._fontStretch) && _fontSize.Equals(other._fontSize) &&
                       _foreground.Equals(other._foreground);
            }

            public override bool Equals(object? obj)
            {
                return obj is FormattedTextParams other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_value, (int) _flowDirection, _fontFamily, _fontStyle, _fontWeight, _fontStretch, _fontSize, _foreground);
            }
        }

        private FormattedTextParams _cachedFormattedTextParams = default;
        private FormattedText? _cachedFormattedText = null;

        private FormattedText GetFormattedText(
            string value,
            FlowDirection flowDirection,
            FontFamily fontFamily,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            double fontSize,
            Brush foreground)
        {
            var parameters = new FormattedTextParams(value, flowDirection, fontFamily, fontStyle, fontWeight,
                fontStretch, fontSize, foreground);

            if (!parameters.Equals(_cachedFormattedTextParams))
            {
                _cachedFormattedTextParams = parameters;
                _cachedFormattedText = GetFormattedTextUncached(value, flowDirection, fontFamily, fontStyle, fontWeight,
                    fontStretch, fontSize, foreground);
            }

            return _cachedFormattedText!;
        }

        private FormattedText GetFormattedTextUncached(
            string value,
            FlowDirection flowDirection,
            FontFamily fontFamily,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            double fontSize,
            Brush foreground) =>
                new FormattedText(value == null ? string.Empty : value,
                    CultureInfo.CurrentUICulture,
                    flowDirection,
                    new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                    fontSize,
                    Foreground,
                    null,
                    TextFormattingMode.Display);
    }
}
