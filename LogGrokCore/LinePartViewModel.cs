namespace LogGrokCore;

public class LinePartViewModel : ViewModelBase
{
   public LinePartViewModel(string source)
    {
        TextModel = new TextModel(source);
        OriginalText = source;
    }

    public TextModel TextModel { get; }
    
    public string OriginalText { get; }
}