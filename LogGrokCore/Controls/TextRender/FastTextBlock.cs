using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogGrokCore.Data;

namespace LogGrokCore.Controls.TextRender
{
    public class FastTextBlock : Control
    {
        private Lazy<PooledList<GlyphLine>>? _textLines;
        private readonly Lazy<GlyphTypeface> _glyphTypeface;

        private static readonly Dictionary<(FontFamily, FontStyle, FontWeight, FontStretch), GlyphTypeface> 
            TypefaceCache = new();
        
        public static DependencyProperty HighlightRegex  = DependencyProperty.RegisterAttached(
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
        
        public FastTextBlock()
        {
            _glyphTypeface = new Lazy<GlyphTypeface>(CreateGlyphTypeface);
        }

        private static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
             typeof(FastTextBlock),
            new FrameworkPropertyMetadata(null, 
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnTextChanged)
        );

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FastTextBlock fastTextBlock)
            {
                fastTextBlock.UpdateTextLine(e.NewValue as string);
            }
        }

        private void UpdateTextLine(string? newText)
        {
            
            if (_textLines is { IsValueCreated: true })
            {
                foreach (var textLine in _textLines.Value)
                {
                    textLine.Dispose();
                }
                _textLines.Value.Dispose();
            }

            if (newText == null)
            {
                _textLines = null;
                return;
            }

            _textLines = new Lazy<PooledList<GlyphLine>>(() =>
            {
                var list = new PooledList<GlyphLine>(16);
                var glyphTypeFace = _glyphTypeface.Value;
                var pixelsPerDip = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
                foreach (var stringRange in newText.Tokenize())
                {
                    list.Add(new GlyphLine(stringRange, glyphTypeFace, FontSize, pixelsPerDip));
                }
                return list;
            });
        }

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            var verticalPosition = 0.0;
            var textLines = _textLines?.Value;

            if (textLines == null) return;
            
            var drawingGeometries = 
                GetHighlightGeometries(Text, 
                    GetHighlightRegex(this));
                
            if(drawingGeometries != null)
                drawingContext.DrawGeometry(Brushes.Moccasin, 
                    new Pen(Brushes.Moccasin, 0), 
                    drawingGeometries);
            
            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;    
            foreach (var textLine in textLines)
            {
                textLine.Render( new Point(0, Math.Round(verticalPosition * pixelsPerDip, MidpointRounding.ToEven) / pixelsPerDip), Foreground, drawingContext);
                verticalPosition += textLine.AdvanceHeight;
            }
        }
        protected override Size MeasureOverride(Size constraint)
        {
            var textLines = _textLines?.Value;
            if (textLines == null) return new Size(0, 0);
            var height = 0.0;
            var width = 0.0;
            
            foreach (var textLine in textLines)
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
            if(!typeface.TryGetGlyphTypeface(out glyphTypeface))
                throw  new NotSupportedException();
            TypefaceCache[key] = glyphTypeface;
            return glyphTypeface;
        }
        
        private Geometry? GetHighlightGeometries(string? text, Regex? regex)
        {
            _cachedGetDrawingGeometries ??=
                Cached.Of<(string? text, Regex? regex), Geometry?>(
                    value => GetDrawingGeometriesUncached(
                                            _textLines?.Value, value.regex));
            return _cachedGetDrawingGeometries((text, regex));
        }

        private Rect Inflate(Rect rect, double inflateValue)
        {
            return new Rect(rect.X - inflateValue, rect.Y - inflateValue, rect.Width + inflateValue * 2,
                rect.Height + inflateValue * 2);
        }
        
        private Geometry? GetDrawingGeometriesUncached(PooledList<GlyphLine>? textLines, Regex? regex)
        {
            if (textLines == null || regex == null || textLines.Count == 0)
            {
                return null;
            }

            var accumulatedGeometry = new GeometryGroup {FillRule = FillRule.Nonzero};

            var y = 0.0;
            foreach (var textLine in textLines)
            {
                var line = textLine.Text.ToString();
                var matches = regex.Matches(line).ToList();
                foreach(var match in matches.Where(m => m.Length > 0))
                {
                    var rect = textLine.GetTextBounds(new Point(0, y), match.Index, match.Length);
                    var geometry = new RectangleGeometry(Inflate(rect, 2), 5, 5);
                    accumulatedGeometry.Children.Add(geometry);
                }
                y += textLine.Size.Height;
            }

            return accumulatedGeometry;
        }
        
        private Func<(string? text, Regex? regex), Geometry?>? _cachedGetDrawingGeometries;
    }
}