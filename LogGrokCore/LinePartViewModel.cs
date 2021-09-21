namespace LogGrokCore
{
    public class LinePartViewModel : ViewModelBase
    {
        private const int MaxLines = 20;

        public LinePartViewModel(string source)
        {
            void SetTrimmedProperties()
            {
                var (text, trimmedLinesCount) = TextOperations.TrimLines(source, MaxLines);
                Text = TextOperations.Normalize(text);
                TrimmedLinesCount = trimmedLinesCount;
                FullText = source;
                IsTrimmedLinesHidden = true;
            }

            void SetUntrimmedProperties()
            {
                Text = source;
                TrimmedLinesCount = 0;
                FullText = source;
                IsTrimmedLinesHidden = false;
            }

            ExpandCommand = new DelegateCommand(() =>
            {
                SetUntrimmedProperties();
                RaiseAllPropertiesChanged();
            });

            CollapseCommand = new DelegateCommand(() =>
            {
                SetTrimmedProperties();
                RaiseAllPropertiesChanged();
            });

            SetTrimmedProperties();
            IsCollapsible = TrimmedLinesCount > 0;
        }

        public bool IsCollapsible { get; }

        public bool IsTrimmedLinesHidden { get; private set; }
        
        public DelegateCommand ExpandCommand { get; }

        public DelegateCommand CollapseCommand { get; }

        public string? Text { get; private set; }
        
        public int TrimmedLinesCount { get; private set; }

        public string? FullText { get; private set; }
    }
}