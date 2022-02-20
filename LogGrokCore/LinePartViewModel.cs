namespace LogGrokCore;

public class LinePartViewModel : ViewModelBase
{
   public LinePartViewModel(int uniqueId, string source)
    {
        TextModel = new TextModel(uniqueId, source);
        OriginalText = source;
    }

    public TextModel TextModel { get; }
    
    public string OriginalText { get; }
}