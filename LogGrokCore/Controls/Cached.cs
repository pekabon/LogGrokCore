using System;

namespace LogGrokCore.Controls
{
    // TODO: use source code generators
    public static class Cached
    {
        private class Storage<TKey, TValue>
        {
            public bool IsEmpty { get; set; }

            public TKey? Key { get; set; }
            public TValue? Value { get; set; }
        }

        public static Func<T, TResult> Of<T, TResult>(Func<T, TResult?> sourceFunc)
        {
            Storage<T, TResult> storage = new();

            TResult? Func(T arg)
            {
                if (storage.IsEmpty || !Equals(arg, storage.Key))
                {
                    storage.Key = arg;
                    storage.Value = sourceFunc(arg);
                    storage.IsEmpty = false;
                }

                return storage.Value;
            }

            return Func;
        }
    }
}