using System;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Filter
{
    internal class FilterSettings
    {
        private readonly Dictionary<int, HashSet<string>> _exclusions = new();
        private readonly Indexer _indexer;
        private readonly LogMetaInformation _metaInformation;
        
        public bool HaveExclusions => _exclusions.Count > 0;
        
        public event Action? ExclusionsChanged;

        public FilterSettings(Indexer indexer, LogMetaInformation metaInformation)
        {
            _indexer = indexer;
            _metaInformation = metaInformation;
        }

        public IReadOnlyDictionary<int, IEnumerable<string>> Exclusions
        {
            get
            {
                return _exclusions.ToDictionary(
                    kv => kv.Key, 
                    kv => kv.Value as IEnumerable<string>);
            }
        }

        public bool this[(int component, string value) arg]
        {
            get
            {
                if (!_exclusions.TryGetValue(arg.component, out var componentExclusions))
                    return true;
                return !componentExclusions.Contains(arg.value);
            }
            set
            {
                var areExclusionsExist = _exclusions.TryGetValue(arg.component, out var componentExclusions);

                if (value && componentExclusions != null)
                {
                    componentExclusions.Remove(arg.value);
                    ExclusionsChanged?.Invoke();
                }
                else
                {
                    if (!areExclusionsExist)
                    {
                        componentExclusions = new HashSet<string>();
                        _exclusions[arg.component] = componentExclusions;
                    }

                    if (componentExclusions?.Add(arg.value) is true)
                    {
                        ExclusionsChanged?.Invoke();
                    }
                }
            }
        }

        public void AddExclusions(int component, IEnumerable<string> componentValuesToExclude)
        {
            var indexedComponent = GetIndexedComponent(component);

            if (!_exclusions.TryGetValue(indexedComponent, out var currentExclusions))
            {
                currentExclusions = new HashSet<string>();
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
            _exclusions[indexedComponent] = componentValuesToExclude.ToHashSet();
            ExclusionsChanged?.Invoke();
        }

        private int GetIndexedComponent(int component) =>
            Array.IndexOf(_metaInformation.IndexedFieldNumbers, component);
    }
}