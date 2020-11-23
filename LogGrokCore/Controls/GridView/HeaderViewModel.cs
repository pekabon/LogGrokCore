using LogGrokCore.Filter;

namespace LogGrokCore.Controls.GridView
{
    public class HeaderViewModel
    {
        public string? FieldHeader { get; init; }
        public FilterViewModel? FilterViewModel { get; init; }

        public bool IsFilteredField => FilterViewModel != null;
    }
}