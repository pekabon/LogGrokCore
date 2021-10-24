using System;
using System.Buffers;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace LogGrokCore.Controls.TextRender
{
    public class SingleThreadedObjectPool<T>
    {
        public SingleThreadedObjectPool(Func<T> factory) => _factory = factory;

        public T Get() => _cachedObjects.TryPop(out var obj) ? obj : _factory();

        public void Return(T obj) => _cachedObjects.Push(obj);

        private readonly Stack<T> _cachedObjects = new Stack<T>();
        private readonly Func<T> _factory;
    }

    public class FastTextBlock : Control
    {
  
        private Lazy<GlyphLine[]>? _textLines;
        private int _lineCount;

        private readonly Lazy<GlyphTypeface> _glyphTypeface;
        private readonly TextFormatter _formatter = TextFormatter.Create(TextFormattingMode.Display);

        private static Dictionary<(FontFamily, FontStyle, FontWeight, FontStretch), GlyphTypeface> 
            _typefaceCache = new();
        public FastTextBlock()
        {
            _glyphTypeface = new Lazy<GlyphTypeface>(() => CreateGlyphTypeface());
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
                foreach (var textLine in _textLines.Value.AsSpan(0, _lineCount))
                {
                    textLine.Dispose();
                }
                ArrayPool<GlyphLine>.Shared.Return(_textLines.Value);
            }

            if (newText == null)
            {
                _textLines = null;
                return;
            }
            
            var strings = newText.Split(Environment.NewLine);
            _lineCount = strings.Length;
            _textLines = new Lazy<GlyphLine[]>(() =>
            {
                var textLinesArray = ArrayPool<GlyphLine>.Shared.Rent(_lineCount);
                for (var i = 0; i < strings.Length; i++)
                {
                    textLinesArray[i] = 
                        new GlyphLine(strings[i], _glyphTypeface.Value, FontSize);
                }
                return textLinesArray;
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
            if (textLines != null)
            {
                foreach (var textLine in textLines.AsSpan(0,_lineCount))
                {
                    textLine.Render( new Point(0, verticalPosition), Foreground, drawingContext);
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

        private GlyphTypeface CreateGlyphTypeface()
        {
            var key = (FontFamily, FontStyle, FontWeight, FontStretch);
            if (_typefaceCache.TryGetValue(key, out var glyphTypeface))
                return glyphTypeface;
            
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            if(!typeface.TryGetGlyphTypeface(out glyphTypeface))
                throw  new NotSupportedException();
            _typefaceCache[key] = glyphTypeface;
            return glyphTypeface;
        }
    }
}