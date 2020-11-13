using System;
using System.Windows.Media.TextFormatting;

namespace LogGrokCore.Controls.TextRender
{
    internal class TextSource : System.Windows.Media.TextFormatting.TextSource
    {
        public TextSource(string text, TextRunProperties textRunProperties)
        {
            Text = text;
            TextRunProperties = textRunProperties;
        }

        private string Text { get; }
        private TextRunProperties TextRunProperties { get; }

        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            if (textSourceCharacterIndex == 0)
                return new TextCharacters(Text, 0, Text.Length, TextRunProperties);
            
            return new TextEndOfParagraph(1);
        }
			
        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
        {
            throw new NotSupportedException();
        }
			
        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            throw new NotSupportedException();
        }
    }
}