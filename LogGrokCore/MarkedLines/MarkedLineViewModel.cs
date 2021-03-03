namespace LogGrokCore.MarkedLines
{

    internal class MarkedLineViewModel : ViewModelBase
    {
        public DocumentViewModel Document { get; }
        public int LineNumber { get; }
        public string Text { get; }

        public MarkedLineViewModel(DocumentViewModel document, int lineNumber, string text)
        {
            Document = document;
            LineNumber = lineNumber;
            Text = text;
        }
    }
}