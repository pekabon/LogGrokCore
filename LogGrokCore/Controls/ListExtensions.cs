using System;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Controls
{
    public static class ListExtensions
    {
        public static T? Search<T>(this List<T> source, Predicate<T> predicate) where T : struct
        {
            var idx = source.FindIndex(predicate);
            if (idx >= 0)
                return source[idx];
            return null;
        }

        public static T? TakeFirst<T>(this IEnumerable<T> source) where T : struct
        {
            if (source.Any())
                return source.First();
            else
                return null;
        }
    }
}
