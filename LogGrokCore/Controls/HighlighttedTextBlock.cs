using System;
using System.Globalization;
using System.Linq;
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

        private Func<FormattedText, Size>? _cachedMeasureOverrideCore; 

        private Size MeasureOverrideCore(FormattedText formattedText)
        {
            _cachedMeasureOverrideCore ??=
                Cached.Of((FormattedText text) => new Size(text.Width, text.Height));

            return _cachedMeasureOverrideCore(formattedText);
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

        private readonly char[] _newLineSeparators = Environment.NewLine.ToCharArray();

        private Func<(string? text, Regex? regex), Geometry?>? _cachedGetDrawingGeometries;

        private Geometry? GetDrawingGeometries(string? text, Regex? regex)
        {
            _cachedGetDrawingGeometries ??=
                Cached.Of<(string? text, Regex? regex), Geometry?>(
                    value => GetDrawingGeometriesUncached(value.text, value.regex));
            return _cachedGetDrawingGeometries((text, regex));
        }

        private Geometry? GetDrawingGeometriesUncached(string? text, Regex? regex)
        {
            if (string.IsNullOrEmpty(text) || regex == null)
            {
                return null;
            }

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
                    GetFormattedTextUncached(line, FlowDirection, FontFamily, FontStyle, FontWeight, 
                        FontStretch, FontSize, Foreground, TextOptions.GetTextFormattingMode(this));
                foreach(var match in matches.Where(m => m.Length > 0))
                {
                    var geometry = ft.BuildHighlightGeometry(new Point(0, y), match.Index, match.Length);
                    accumulatedGeometry.Children.Add(geometry);
                }
                y += ft.Height;
            }

            return accumulatedGeometry;
        }

        private FormattedText GetFormattedText()
        {
            return GetFormattedText(Text, FlowDirection, FontFamily, FontStyle, FontWeight, 
                FontStretch, FontSize, Foreground, TextOptions.GetTextFormattingMode(this));
        }

        private Func<(string value,
            FlowDirection flowDirection,
            FontFamily fontFamily,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            double fontSize,
            Brush foreground,
            TextFormattingMode textFormattingMode), FormattedText>? _cachedGetFormattedText;

        private FormattedText GetFormattedText(
            string value,
            FlowDirection flowDirection,
            FontFamily fontFamily,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            double fontSize,
            Brush foreground,
            TextFormattingMode textFormattingMode)
        {
            _cachedGetFormattedText ??=
                Cached.Of(((string value,
                        FlowDirection flowDirection,
                        FontFamily fontFamily,
                        FontStyle fontStyle,
                        FontWeight fontWeight,
                        FontStretch fontStretch,
                        double fontSize,
                        Brush foreground,
                        TextFormattingMode textFormattingMode) p) =>
                    GetFormattedTextUncached(p.value, p.flowDirection, p.fontFamily, p.fontStyle, p.fontWeight,
                        p.fontStretch, p.fontSize, p.foreground, p.textFormattingMode));

            var parameters = (value, flowDirection, fontFamily, fontStyle, fontWeight,
                fontStretch, fontSize, foreground, textFormattingMode);
            return _cachedGetFormattedText(parameters);
        }

        private static FormattedText GetFormattedTextUncached(
            string value,
            FlowDirection flowDirection,
            FontFamily fontFamily,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            double fontSize,
            Brush foreground,
            TextFormattingMode textFormattingMode) =>
                new(value,
                    CultureInfo.CurrentUICulture,
                    flowDirection,
                    new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                    fontSize,
                    foreground,
                    null,
                    textFormattingMode);
    }
}
