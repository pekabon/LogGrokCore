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

        public string Pattern { get; }
        public bool IsCaseSensitive { get; }
        public bool UseRegex { get; }

        public SearchPattern Clone()
        {
            return new SearchPattern(Pattern, IsCaseSensitive, UseRegex);
        }

        public Regex GetRegex(RegexOptions regexAdditionalOptions)
        {
            var regexOptions = IsCaseSensitive ? RegexOptions.None | RegexOptions.IgnoreCase : RegexOptions.None;
            var pattern = UseRegex ? Pattern : Regex.Escape(Pattern);
            return new Regex(pattern, regexOptions | regexAdditionalOptions);
        }
    }
}