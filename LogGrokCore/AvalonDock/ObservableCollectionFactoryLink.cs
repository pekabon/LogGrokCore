using Functional.Maybe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace LogGrokCore.AvalonDock
{
    public class ObservableCollectionFactoryLink<TTarget> where TTarget : class
    {
        public IList SourceCollection { get; private set; }

        public ObservableCollection<TTarget> TargetCollection { get; private set; }

        public Func<object, TTarget> Factory { get; private set; }
        
        private Dictionary<object, TTarget> _sourceToTargetMapping = new Dictionary<object, TTarget>();
        
        public ObservableCollectionFactoryLink(IList source, ObservableCollection<TTarget> target, 
            Func<object,TTarget> factory)
        {
            SourceCollection = source;
            TargetCollection = target;
            Factory = factory;

            TargetCollection.Clear();
            foreach (var s in SourceCollection)
            {
                var t = Factory(s);
                _sourceToTargetMapping[s] = t;
                TargetCollection.Add(t);
            }

            void SyncSourceChanges(IList source, ObservableCollection<TTarget> target)
            {
                bool haveSource(TTarget trgt)
                {
                    var src = _sourceToTargetMapping.First(k => object.ReferenceEquals(k.Value, trgt)).Key;
                    return source.Contains(src);
                }

                var itemsToAdd = source.Cast<object>().Except(_sourceToTargetMapping.Keys).Select(s => (s, Factory(s))).ToList();
                var itemsToRemove = target.Where(t => !haveSource(t)).ToList();

                foreach (var i in itemsToAdd)
                {
                    var (s, t) = i;
                    _sourceToTargetMapping[s] = t;
                    target.Add(t);
                }

                foreach (var i in itemsToRemove)
                {
                    target.Remove(i);

                    foreach (var s in SourceFromTarget(i).ToEnumerable())
                    {
                        _sourceToTargetMapping.Remove(s);
                    }
                }
            }
        

            void SyncTargetChanges(IList source, ObservableCollection<TTarget> target)
            {
                var itemsToSave = _sourceToTargetMapping.Where(k => target.Contains(k.Value)).Select(k => k.Key).ToList();
                var itemsToRemove = source.Cast<object>().Except(itemsToSave).ToList();

                foreach(var i in itemsToRemove)
                {
                    source.Remove(i);
                    _sourceToTargetMapping.Remove(i);
                }
            }

            ((INotifyCollectionChanged)SourceCollection).CollectionChanged += (_, __) => SyncSourceChanges(SourceCollection, TargetCollection);
            TargetCollection.CollectionChanged += (_, __) => SyncTargetChanges(SourceCollection, TargetCollection);
        }

        public Maybe<object> SourceFromTarget(TTarget target) 
        {
            var result = _sourceToTargetMapping.FirstOrDefault(kv => ReferenceEquals(kv.Value, target));

            if (result.Equals(default(KeyValuePair<object, TTarget>)))
                return Maybe<object>.Nothing;
            else
                return result.Key.ToMaybe();
        }

        public Maybe<TTarget> TargetFromSource(object source) 
        {
            if (source == null || !_sourceToTargetMapping.ContainsKey(source))
                return Maybe<TTarget>.Nothing;
            else
                return _sourceToTargetMapping[source].ToMaybe();
        }        
    }
}
