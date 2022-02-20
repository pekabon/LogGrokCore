namespace LogGrokCore
{
    public class LogHeaderViewModel : ItemViewModel
    {
        public LinePartViewModel Text { get; }

        public  LogHeaderViewModel(string text) => Text = new LinePartViewModel(-1, text);

        public override string ToString() => Text.OriginalText;
    }
}