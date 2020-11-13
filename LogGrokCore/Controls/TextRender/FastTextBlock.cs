using System;
using System.Buffers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace LogGrokCore.Controls.TextRender
{
    public struct GlyphLine
    {
        private GlyphRun? _run;

        public Size Size { get; }

        public GlyphLine(string text, GlyphTypeface typeface, double fontSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                _run = null;
                return;
            }
            var glyphIndexes = new ushort[text.Length];
            var advanceWidths = new double[text.Length];
            double totalWidth = 0;
            for (int n = 0; n < text.Length; n++) {
                ushort glyphIndex;
                
                typeface.CharacterToGlyphMap.TryGetValue(text[n], out glyphIndex);
                glyphIndexes[n] = glyphIndex;
                var width = typeface.AdvanceWidths[glyphIndex] * fontSize;
                
                advanceWidths[n] = width;
                totalWidth += width;
            }

            var height = typeface.Height* fontSize;
            
            Size = new Size(totalWidth, height);
            _run = new GlyphRun(typeface,
                bidiLevel: 0,
                isSideways: false,
                renderingEmSize: fontSize,
                glyphIndices: glyphIndexes,
                baselineOrigin: new Point(0, Math.Round(typeface.Baseline * fontSize)),
                advanceWidths: advanceWidths,
                glyphOffsets: null,
                characters: null,
                deviceFontName: null,
                clusterMap: null,
                caretStops: null,
                language: null);
            
            
        }

        public void Render(Point position, Brush brush, DrawingContext drawingContext)
        {
            if (_run == null) return; 
            drawingContext.PushTransform(new TranslateTransform(position.X, position.Y));
            drawingContext.DrawGlyphRun(brush, _run);
            drawingContext.Pop();
        }
    }

    public class FastTextBlock : Control
    {
        private Lazy<GlyphLine[]>? _textLines;
        private int _lineCount;
        private readonly Lazy<Typeface> _typeFace;
        private readonly Lazy<GlyphTypeface> _glyphTypeface;
        private readonly TextFormatter _formatter = TextFormatter.Create(TextFormattingMode.Display);

        public FastTextBlock()
        {
            _typeFace = new Lazy<Typeface>(CreateTypeface);
            _glyphTypeface = new Lazy<GlyphTypeface>(() => CreateGlyphTypeface(_typeFace.Value));
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
            if (_textLines != null)
            {
                foreach (var textLine in _textLines.Value)
                {
                    //textLine.Dispose();
                }
            }

            if (newText == null)
            {
                _textLines = null;
                return;
            }
            
            var strings = newText.Split(Environment.NewLine);
            // var textProperties = new System.Windows.Media.TextFormatting.TextRunProperties(
            //     typeface: _typeFace.Value,
            //     fontRenderingEmSize: FontSize,
            //     foregroundBrush: Foreground,
            //     backgroundBrush: Background,
            //     cultureInfo: CultureInfo.CurrentCulture);

            if (_textLines != null && _textLines.IsValueCreated)
            {
                var textLines = _textLines.Value;
                _textLines = null;
                ArrayPool<GlyphLine>.Shared.Return(textLines);
                foreach (var textLine in textLines.AsSpan(0, _lineCount))
                {
                    //textLine.Dispose();
                }
            }

            _lineCount = strings.Length;
            _textLines = new Lazy<GlyphLine[]>(() =>
            {
                var textLinesArray = ArrayPool<GlyphLine>.Shared.Rent(_lineCount);
                for (var i = 0; i < strings.Length; i++)
                {
                    
                    textLinesArray[i] = 
                        new GlyphLine(strings[i], _glyphTypeface.Value, FontSize);
                    //CreateTextLine(strings[i], textProperties);
                }

                return textLinesArray;
            });
        }

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        

        // private TextLine CreateTextLine(string text, System.Windows.Media.TextFormatting.TextRunProperties textProperties)
        // {
        //     var textSource = new System.Windows.Media.TextFormatting.TextSource(text, textProperties);
        //     return _formatter.FormatLine(textSource, 0, 0,
        //         new System.Windows.Media.TextFormatting.TextParagraphProperties(textProperties), null);
        // }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var verticalPosition = 0.0;
            var textLines = _textLines?.Value;
            if (textLines != null)
            {
                foreach (var textLine in textLines.AsSpan(0,_lineCount))
                {
                    textLine.Render( new Point(0, verticalPosition), Foreground, drawingContext);
                   // textLine.Draw(drawingContext, new Point(0, verticalPosition), InvertAxes.None);
                    verticalPosition += textLine.Size.Height;
                }
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var textLines = _textLines?.Value;
            if (textLines == null) return new Size(0, 0);
            var height = 0.0;
            var width = 0.0;
            
            foreach (var textLine in textLines.AsSpan(0, _lineCount))
            {
                height += textLine.Size.Height;
                width = Math.Max(width, textLine.Size.Width);
            }
            
            return new Size(width, height);
        }

        private Typeface CreateTypeface()
        {
            return new Typeface(FontFamily,FontStyle, FontWeight, FontStretch);
        }

        private GlyphTypeface CreateGlyphTypeface(Typeface typeface)
        {
            if(!typeface.TryGetGlyphTypeface(out var glyphTypeface))
                throw  new NotSupportedException();
            return glyphTypeface;
        }
    }
}