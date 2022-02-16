using LogGrokCore.Colors;

namespace LogGrokCore.MarkedLines
{
    public class MarkedLineViewModel : BaseLogLineViewModel
    {
        public DocumentViewModel Document { get; }
        public LinePartViewModel Text { get; }
        public ColorSettings ColorSettings { get; }

        public MarkedLineViewModel(DocumentViewModel document, int lineNumber, string text)
            : base(lineNumber, document.MarkedLines)        
        {
            Document = document;
    
            Text = new LinePartViewModel(text);
            ColorSettings = document.ColorSettings;
        }

        public override string ToString() => Text.OriginalText ?? string.Empty;
    }
}