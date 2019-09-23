using System.Text.RegularExpressions;

namespace LogGrokCore.Search
{
    internal struct SearchPattern
    {
        public SearchPattern(string searchText, in bool isCaseSensitive, in bool useRegex)
        {
            Pattern = searchText;
            IsCaseSensitive = isCaseSensitive;
            UseRegex = useRegex;
        }

        public string Pattern { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool UseRegex { get; set; }

        public Regex GetRegex(RegexOptions regexAdditionalOptions)
        {
            var regexOptions = IsCaseSensitive ? RegexOptions.None | RegexOptions.IgnoreCase : RegexOptions.None;
            var pattern = UseRegex ? Pattern : Regex.Escape(Pattern);
            return new Regex(pattern, regexOptions | regexAdditionalOptions);
        }
    }
}