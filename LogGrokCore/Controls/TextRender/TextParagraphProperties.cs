using System.Windows;
using System.Windows.Media.TextFormatting;

namespace LogGrokCore.Controls.TextRender
{
    internal class TextParagraphProperties : System.Windows.Media.TextFormatting.TextParagraphProperties
    {
        public TextParagraphProperties(TextRunProperties defaultTextRunProperties, 
            TextWrapping textWrapping = TextWrapping.NoWrap, 
            double tabSize = 40.0)
        {
            DefaultTextRunProperties = defaultTextRunProperties;
            TextWrapping = textWrapping;
            DefaultIncrementalTab= tabSize;
        }

        public override double DefaultIncrementalTab { get; } = 40.0;
        public override FlowDirection FlowDirection => FlowDirection.LeftToRight;
        public override TextAlignment TextAlignment => TextAlignment.Left;
        public override double LineHeight => double.NaN;
        public override bool FirstLineInParagraph => false;
        public override System.Windows.Media.TextFormatting.TextRunProperties DefaultTextRunProperties { get; }
        public override TextWrapping TextWrapping { get; }
        public override TextMarkerProperties? TextMarkerProperties => null;
        public override double Indent => 0;
    }
}