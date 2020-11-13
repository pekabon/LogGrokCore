using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace LogGrokCore.Controls.TextRender
{
    internal sealed class TextRunProperties : System.Windows.Media.TextFormatting.TextRunProperties
    {
        public TextRunProperties(Typeface typeface, double fontRenderingEmSize, Brush foregroundBrush,
            Brush backgroundBrush, CultureInfo cultureInfo)
        {
            Typeface = typeface;
            FontRenderingEmSize = fontRenderingEmSize;
            ForegroundBrush = foregroundBrush;
            BackgroundBrush = backgroundBrush;
            CultureInfo = cultureInfo;
        }
        public override Typeface Typeface { get; }
        public override double FontRenderingEmSize { get; }
        public override double FontHintingEmSize => FontRenderingEmSize;
        public override TextDecorationCollection? TextDecorations => null;
        public override Brush ForegroundBrush { get; }
        public override Brush BackgroundBrush { get; }
        public override CultureInfo CultureInfo { get; }
        public override TextEffectCollection? TextEffects => null;
    }
}