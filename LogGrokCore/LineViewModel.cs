namespace LogGrokCore
{
    public class LineViewModel
    {
        private string _sourceString;

        public LineViewModel(string sourceString) => _sourceString = sourceString;

        public string GetValue(string valueName)
        {
            if (valueName == "Text")
                return _sourceString;
            return valueName;
        }
    }
}