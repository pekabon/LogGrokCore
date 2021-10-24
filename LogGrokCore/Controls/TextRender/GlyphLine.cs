using System;
using System.Windows;
using System.Windows.Media;
using LogGrokCore.Data;

namespace LogGrokCore.Controls.TextRender
{
    public class GlyphLine : IDisposable
    {
        private GlyphRun? _run;
        public Size Size { get; private set; }

        private readonly PooledList<ushort> _glyphIndices = new();
        private readonly PooledList<double> _advanceWidths = new();
        
        public GlyphLine(ReadOnlySpan<char> text, GlyphTypeface typeface, double fontSize)
        {
            if (text.Length == 0)
            {
                _run = null;
                return;
            }
            
            _glyphIndices = new PooledList<ushort>(text.Length);
            _advanceWidths = new PooledList<double>(text.Length);
            
            double totalWidth = 0;

            var indexOfGlyph = 0;
            for (var n = 0; n < text.Length; n++, indexOfGlyph++) {

                ushort glyphIndex;
                double width;
                if (text[n] == '\t')
                {
                    while (indexOfGlyph % 7 != 0)
                    {
                        typeface.CharacterToGlyphMap.TryGetValue(' ', out glyphIndex);
                        _glyphIndices.Add(glyphIndex);
                        width = typeface.AdvanceWidths[glyphIndex] * fontSize;
                        _advanceWidths.Add(width);
                        totalWidth += width;
                        indexOfGlyph++;
                    }
                    continue;
                }
                typeface.CharacterToGlyphMap.TryGetValue(text[n], out glyphIndex);
                _glyphIndices.Add(glyphIndex);
                width = typeface.AdvanceWidths[glyphIndex] * fontSize;
                _advanceWidths.Add(width);
                totalWidth += width;
            }

            var height = typeface.Height* fontSize;
            
            Size = new Size(totalWidth, height);
            _run = new GlyphRun(typeface,
                bidiLevel: 0,
                isSideways: false,
                renderingEmSize: fontSize,
                glyphIndices: _glyphIndices,
                baselineOrigin: new Point(0, Math.Round(typeface.Baseline * fontSize)),
                advanceWidths: _advanceWidths,
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

        public void Dispose()
        {
            _advanceWidths.Dispose();
            _glyphIndices.Dispose();
        }
    }
}