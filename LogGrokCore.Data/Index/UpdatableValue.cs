using System;
using System.Collections.Generic;

namespace LogGrokCore.Data.Index
{
    public class UpdatableValue<TResult>
    {
        private readonly IUpdatableValueImpl _impl;

        private interface IUpdatableValueImpl
        {
            public TResult Value { get; }
        }
        private class UpdatableValueCoreImpl<TSource> : IUpdatableValueImpl
        {
            private readonly Func<TSource> _getter;
            private readonly Func<TSource, TResult> _converter;
            private TSource _source;
            private TResult _cachedResult;

            public UpdatableValueCoreImpl(Func<TSource> getter, Func<TSource, TResult> converter)
            {
                _getter = getter;
                _converter = converter;
                _source = getter();
                _cachedResult = converter(_source);
            }

            public TResult Value
            {
                get
                {
                    var newSource = _getter();
                    if (EqualityComparer<TSource>.Default.Equals(newSource, _source)) return _cachedResult;
                
                    _source = newSource;
                    _cachedResult = _converter(_source);
                    return _cachedResult;
                }
            }
        }

        private UpdatableValue(IUpdatableValueImpl impl)
        {
            _impl = impl;
        }
        
        public static UpdatableValue<TResult> Create<TSource>(Func<TSource> getter, Func<TSource, TResult> converter)
        {
            return new(new UpdatableValueCoreImpl<TSource>(getter, converter));
        }
        public TResult Value => _impl.Value;
    }
    
    public static class UpdatableValue
    {
        public static UpdatableValue<TResult> Create<TSource, TResult>(Func<TSource> getter, Func<TSource, TResult> converter)
        {
            return UpdatableValue<TResult>.Create(getter, converter);
        }
        
        public static UpdatableValue<TSource> Create<TSource>(Func<TSource> getter)
        {
            return UpdatableValue<TSource>.Create(getter, i=> i);
        }

        public static UpdatableValue<TResult2> Map<TResult, TResult2>(
            this UpdatableValue<TResult> updatableValue,
            Func<TResult, TResult2> converter)
        {
            return Create(() => updatableValue.Value, converter);
        }
    }
}