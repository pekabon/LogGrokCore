using System;

namespace LogGrokCore.Filter
{
    internal class ElementViewModel : ViewModelBase
    {
        public string Name { get; }

        public string Category { get; }
        
        public bool IsActive { get; }

        public int Percent => _percentGetter();

        private readonly Func<int> _percentGetter;

        public ElementViewModel(string name, string category, bool isActive, Func<int> percentGetter)
        {
            Name = name;
            Category = category;
            IsActive = isActive;
            
            _percentGetter = percentGetter;
        }
    }
}
