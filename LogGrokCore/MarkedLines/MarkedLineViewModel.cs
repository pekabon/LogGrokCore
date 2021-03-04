using LogGrokCore.Colors;
using LogGrokCore.Controls;

namespace LogGrokCore.MarkedLines
{

    internal class MarkedLineViewModel : BaseLogLineViewModel
    {
        public DocumentViewModel Document { get; }
        public string Text { get; }
        public ColorSettings ColorSettings { get; }

        public MarkedLineViewModel(DocumentViewModel document, int lineNumber, string text)
            : base(lineNumber, document.MarkedLines)        
        {
            Document = document;
    
            Text = text;
            ColorSettings = document.ColorSettings;
        }

        public override string ToString() => Text;
    }
}