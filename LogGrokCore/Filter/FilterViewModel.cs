using System.ComponentModel;

namespace LogGrokCore.Filter
{
    internal class FilterViewModel : ViewModelBase
    {
        public FilterViewModel(string fieldName)
        {
        }

        public bool IsFilterApplied { get; private set; }
    }
}