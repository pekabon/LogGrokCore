using System;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Filter
{
    internal class FilterSettings
    {
        private readonly Dictionary<int, IReadOnlyList<string>> _exclusions = 
            new Dictionary<int, IReadOnlyList<string>>();
        private readonly Indexer _indexer;
        private readonly LogMetaInformation _metaInformation;
        
        public bool HaveExclusions => _exclusions.Count > 0;
        
        public event Action? ExclusionsChanged;

        public FilterSettings(Indexer indexer, LogMetaInformation metaInformation)
        {
            _indexer = indexer;
            _metaInformation = metaInformation;
        }

        public IReadOnlyDictionary<int, IReadOnlyList<string>> Exclusions => _exclusions;
        
        public void AddExclusions(int component, IEnumerable<string> componentValuesToExclude)
        {
            var indexedComponent = GetIndexedComponent(component);

            if (!_exclusions.TryGetValue(indexedComponent, out var currentExclusions))
            {
                currentExclusions = new List<string>();
            }

            SetExclusions(indexedComponent, currentExclusions.Concat(componentValuesToExclude));
        }

        public void ExcludeAllExcept(int component, IEnumerable<string> componentValuesToInclude)
        {
            var indexedComponent = GetIndexedComponent(component);

            var exclusions = _indexer.GetAllComponents(indexedComponent).Except(componentValuesToInclude);
            SetExclusions(indexedComponent , exclusions);
        }

        public void ClearAllExclusions()
        {
            _exclusions.Clear();
            ExclusionsChanged?.Invoke();
        }

        private void SetExclusions(int indexedComponent, IEnumerable<string> componentValuesToExclude)
        {
            _exclusions[indexedComponent] = componentValuesToExclude.ToList();
            ExclusionsChanged?.Invoke();
        }

        private int GetIndexedComponent(int component) =>
            Array.IndexOf(_metaInformation.IndexedFieldNumbers, component);
    }
}