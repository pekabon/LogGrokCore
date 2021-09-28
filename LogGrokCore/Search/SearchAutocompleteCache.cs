using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LogGrokCore.Search
{
    public struct AutocompleteItem
    {
        public string Value { get; set; }
        public DateTime LastUsed { get; set; }
        public int UseCount { get; set; }
    }
    public class SearchAutocompleteCache
    {
        private const int MaxAutocompleteItems = 1024;
        
        private readonly string _cacheFileName =  DataFilePathProvider.GetDataFileFullPath("AutocompleteCache.json");
        
        private readonly List<AutocompleteItem> _items = new();
        public IEnumerable<string> Items => _items.Select(v => v.Value);

        public SearchAutocompleteCache()
        {
            var fullPath = _cacheFileName;
            try
            {
                if (!File.Exists(fullPath)) return;
                using var stream = File.OpenRead(fullPath);
                _items = JsonSerializer.Deserialize<List<AutocompleteItem>>(stream) 
                         ?? new List<AutocompleteItem>();
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        public void Save()
        {
            try
            {
                using var createStream = File.Create(_cacheFileName);
                var options = new JsonSerializerOptions { WriteIndented = true };
                JsonSerializer.Serialize(createStream, _items, options);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        public void Add(string value)
        {
            var now = DateTime.UtcNow;
            var existingItemIndex = _items.FindIndex(v => v.Value == value);
            if (existingItemIndex == -1)
            {
                _items.Add(new AutocompleteItem { Value = value, LastUsed = now, UseCount = 1 });
            }
            else
            {
                var existing = _items[existingItemIndex];
                
                var currentCount = existing.UseCount;
                _items[existingItemIndex] = new AutocompleteItem { Value=value, LastUsed = now, UseCount = currentCount + 1 };
            }            
            
            _items.Sort((x, y) =>
            {
                var useCountComparison = -x.UseCount.CompareTo(y.UseCount);
                return useCountComparison != 0 ? useCountComparison : -x.LastUsed.CompareTo(y.LastUsed);
            });

            while (_items.Count > MaxAutocompleteItems)
            {
                _items.RemoveAt(_items.Count -1);
            }
        }
    }
}