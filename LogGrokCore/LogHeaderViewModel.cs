namespace LogGrokCore
{
    public class LogHeaderViewModel : ItemViewModel
    {
        public LinePartViewModel Text { get; }

        public  LogHeaderViewModel(string text) => Text = new LinePartViewModel(text);

        public override string ToString() => Text.FullText ?? string.Empty;
    }
}