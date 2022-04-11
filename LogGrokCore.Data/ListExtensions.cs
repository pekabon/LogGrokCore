using System;
using System.Collections.Generic;

namespace LogGrokCore.Data
{
    public static class ListExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
        {
            var i = 0;
            foreach(var element in self)
            {
                if( Equals( element, elementToFind))
                    return i;
                i++;
            }
            return -1;
        }
        
        public static int BinarySearch<TElement, TValue>(this IReadOnlyList<TElement> lst, int startIndex, int length, TValue value, 
            Func<TElement, TValue, int> comparer)  
        {
            var i = startIndex;
            var num = startIndex + length - 1;

            var result = 0;
            while (i <= num)
            {
                var num2 = i + (num - i >> 1);
                var num3 = comparer(lst[num2], value);
                if (num3 == 0)
                {
                    result = num2;
                    break;
                }

                if (num3 < 0)
                {
                    i = num2 + 1;
                }
                else
                {
                    num = num2 - 1;
                }
                result = ~i;
            }

            return result;
        }

        public static int BinarySearch<TElement, TValue>(this IReadOnlyList<TElement> lst, TValue value, 
            Func<TElement, TValue, int> comparer)
        {
            return lst.BinarySearch(0, lst.Count, value, comparer);
        }
    }
}