using System.Collections.Generic;

namespace LogGrokCore
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Yield<T>(this T source) 
        {
            yield return source;
        }
    }
}