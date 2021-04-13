using System;
using System.Threading.Tasks;

namespace LogGrokCore.Filter
{
    public class ElementViewModel : ViewModelBase
    {
        public string Name { get; }

        public bool IsActive
        {
            get => _filterSettings[(_componentIndex, Name)];
            set
            {
                if (_filterSettings[(_componentIndex, Name)] == value) return;
                _filterSettings[(_componentIndex, Name)] = value;
                InvokePropertyChanged();
            } 
        }

        public int Percent
        {
            get
            {
                var now = DateTime.Now;
                if (now - _percentCacheTime > TimeSpan.FromSeconds(3))
                {
                    _percentCacheTime = now;
                    UpdateCachedPercent(now);
                }

                return _cachedPercent;
            }
        }

        async void UpdateCachedPercent(DateTime timeStamp)
        {
            _cachedPercent = await Task.Factory.StartNew(() => _percentGetter());
            InvokePropertyChanged(nameof(Percent));
        }

        private readonly Func<int> _percentGetter;
        private readonly FilterSettings _filterSettings;
        private readonly int _componentIndex;
        
        private int _cachedPercent;
        private DateTime _percentCacheTime = DateTime.MinValue;
        
        public ElementViewModel(
            string name,
            int componentIndex, 
            FilterSettings filterSettings, 
            Func<int> percentGetter)
        {
            Name = name;

            _componentIndex = componentIndex;
            _filterSettings = filterSettings;
            _filterSettings.ExclusionsChanged += () => InvokePropertyChanged(nameof(IsActive));
            _percentGetter = percentGetter;
        }
    }
}
