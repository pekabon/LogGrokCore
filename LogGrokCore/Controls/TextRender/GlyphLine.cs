using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LogGrokCore.Data;

namespace LogGrokCore.Controls.TextRender
{
    public class GlyphLine : IDisposable
    {
        private readonly GlyphRun? _run;
        public Size Size { get; }
        public double AdvanceHeight { get; }

        private readonly PooledList<ushort> _glyphIndices = new();
        private readonly PooledList<double> _advanceWidthsForGlyph = new();
        private readonly PooledList<double> _advanceWidthForChar = new();

        public StringRange Text { get; }
        
        public int GetNearestTextPosition(double relativeHorizontalPosition)
        {
            if (relativeHorizontalPosition <= 0)
            {
                return 0;
            }

            var currentOffset = 0.0;
            for (var i = 0; i < _advanceWidthForChar.Count; i++)
            {
                var currentCharWidth = _advanceWidthForChar[i];
                currentOffset += currentCharWidth;
                if (!(relativeHorizontalPosition <= currentOffset)) continue;
                if (relativeHorizontalPosition + currentCharWidth / 2.0 < currentOffset)
                    return i;
                return i + 1;
            }
            return _advanceWidthForChar.Count;
        }

        public Rect GetTextBounds(Point startPoint, int firstTextSourceCharacterIndex, int textLength)
        {
            var height = Size.Height;
            var start = _advanceWidthForChar.Take(firstTextSourceCharacterIndex).Sum();
            var width = _advanceWidthForChar.Skip(firstTextSourceCharacterIndex).Take(textLength).Sum();

            return new Rect(start + startPoint.X, startPoint.Y, width, height);
        }

        public GlyphLine(StringRange text, GlyphTypeface typeface, double fontSize, float pixelsPerDip)
        {
            Text = text;
            var span = text.Span;
            if (text.Length == 0)
            {
                _run = null;
                return;
            }
            
            _glyphIndices = new PooledList<ushort>(text.Length);
            _advanceWidthsForGlyph = new PooledList<double>(text.Length);
            
            double totalWidth = 0;

            var indexOfGlyph = 0;
            for (var n = 0; n < text.Length; n++, indexOfGlyph++) {

                ushort glyphIndex;
                double width;
                if (span[n] == '\t')
                {
                    var advanceWidthForChar = 0.0;

                    var spaceCount = indexOfGlyph % 8 == 0 ? 8 : (8 - indexOfGlyph % 8); 
                    while (spaceCount > 0)
                    {
                        typeface.CharacterToGlyphMap.TryGetValue(' ', out glyphIndex);
                        _glyphIndices.Add(glyphIndex);
                        width = typeface.AdvanceWidths[glyphIndex] * fontSize;
                        
                        _advanceWidthsForGlyph.Add(width);
                        advanceWidthForChar += width;
                        totalWidth += width;
                        indexOfGlyph++;
                        spaceCount--;
                    }

                    _advanceWidthForChar.Add(advanceWidthForChar);
                    continue;
                }
                
                typeface.CharacterToGlyphMap.TryGetValue(span[n], out glyphIndex);
                _glyphIndices.Add(glyphIndex);
                width = typeface.AdvanceWidths[glyphIndex] * fontSize;
                AdvanceHeight = Math.Max(AdvanceHeight, typeface.AdvanceHeights[glyphIndex] * fontSize);
                _advanceWidthsForGlyph.Add(width);
                _advanceWidthForChar.Add(width);
                totalWidth += width;
            }
            
            var height = typeface.Height* fontSize;
            
            _run = new GlyphRun(typeface,
                bidiLevel: 0,
                isSideways: false,
                renderingEmSize: fontSize,
                glyphIndices: _glyphIndices,
                baselineOrigin: new Point(0, Math.Round(typeface.Baseline * fontSize)),
                advanceWidths: _advanceWidthsForGlyph,
                glyphOffsets: null,
                characters: null,
                deviceFontName: null,
                clusterMap: null,
                caretStops: null,
                language: null, pixelsPerDip:pixelsPerDip);

            Size = new Size(totalWidth, height);
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
            _advanceWidthsForGlyph.Dispose();
            _advanceWidthForChar.Dispose();
            _glyphIndices.Dispose();
        }

        public (int start, int length)? Selection { get; private set; }

        public bool SetSelection(int start, int length)
        {
            if (Selection == (start, length)) return false;
            Selection = (start, length);
            return true;
        }

        public bool ResetSelection()
        {
            var changed = Selection != null;
            Selection = null;
            return changed;
        }
    }
}