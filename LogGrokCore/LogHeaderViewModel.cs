namespace LogGrokCore
{
    public class LogHeaderViewModel : ItemViewModel
    {
        public string Text { get; }

        public  LogHeaderViewModel(string text) => Text = text;

        public override string ToString() => Text;
    }
}