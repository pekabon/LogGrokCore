using System;

namespace LogGrokCore.Filter
{
    internal class ElementViewModel : ViewModelBase
    {
        public string Name { get; }

        public bool IsActive
        {
            get => _isActive;
            set => SetAndRaiseIfChanged(ref _isActive, value);
        }

        public int Percent => _percentGetter();

        private readonly Func<int> _percentGetter;
        private bool _isActive;

        public ElementViewModel(string name, bool isActive, Func<int> percentGetter)
        {
            Name = name;
            
            IsActive = isActive;
            
            _percentGetter = percentGetter;
        }
    }
}
