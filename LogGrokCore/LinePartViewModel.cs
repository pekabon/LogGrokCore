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
                Text = TextOperations.Normalize(text, ApplicationSettings.Instance().ViewSettings);
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

            void SetHideJsonProperties()
            {
                Text = TextOperations.Normalize(source, ApplicationSettings.Instance().ViewSettings);
                IsJsonViewHidden = true;
            }

            void SetUnHideJsonProperties()
            {
                Text = TextOperations.ExpandInlineJson(source);
                IsJsonViewHidden = false;
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

            HideJsonCommand = new DelegateCommand(() =>
            {
                SetHideJsonProperties();
                RaiseAllPropertiesChanged();
            });

            ViewJsonCommand = new DelegateCommand(() =>
            {
                SetUnHideJsonProperties();
                RaiseAllPropertiesChanged();
            });

            IsExistJson = TextOperations.IsExistsInlineJson(source);
            if (IsExistJson)
            {
                SetHideJsonProperties();
            }
            else
            {
                SetTrimmedProperties();
                IsCollapsible = TrimmedLinesCount > 0;
            }
        }

        public bool IsCollapsible { get; }

        public bool IsExistJson { get; }

        public bool IsTrimmedLinesHidden { get; private set; }

        public bool IsJsonViewHidden { get; private set; }

        public DelegateCommand ExpandCommand { get; }

        public DelegateCommand CollapseCommand { get; }

        public DelegateCommand ViewJsonCommand { get; }

        public DelegateCommand HideJsonCommand { get; }

        public string? Text { get; private set; }
        
        public int TrimmedLinesCount { get; private set; }

        public string? FullText { get; private set; }
    }
}