using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Data.Index
{
    public static class CollectionUtils
    {
        public static IEnumerable<int> MergeSorted(IEnumerable<IEnumerable<int>> collections)
        {
            var cursors =
                collections
                    .Select(c => c.GetEnumerator())
                    .Where(e => e.MoveNext());
            return MergeSorted(cursors);
        }

        public static IEnumerable<T> MergeSorted<T>(IEnumerable<IEnumerator<T>> cursors,
            Func<T, T, bool> isNext) where T : IComparable<T>
        {
            var heap = new PriorityQueue<IEnumerator<T>, T>();

            foreach (var cursor in cursors)
            {
                heap.Enqueue(cursor, cursor.Current);
            }
            
            while (heap.Count > 0)
            {
                var head = heap.Dequeue();
                bool haveNext;
                T currentValue;
                do
                {
                    currentValue = head.Current;
                    yield return currentValue;
                    haveNext = head.MoveNext();
                } while (haveNext &&
                         isNext(currentValue, head.Current));

                if (haveNext)
                {
                    heap.Enqueue(head, head.Current);
                }
                else
                {
                    head.Dispose();
                }
            }
        }

        private static IEnumerable<int> MergeSorted(IEnumerable<IEnumerator<int>> cursors)
        {
            var heap = new PriorityQueue<IEnumerator<int>, int>();
            foreach (var cursor in cursors)
            {
                heap.Enqueue(cursor, cursor.Current);
            }
            
            while (heap.Count > 0)
            {
                var head = heap.Dequeue();
                bool haveNext;
                int currentValue;
                do
                {
                    currentValue = head.Current;
                    yield return currentValue;
                    haveNext = head.MoveNext();
                } while (haveNext && head.Current == currentValue + 1);

                if (haveNext)
                {
                    heap.Enqueue(head, head.Current);
                }
                else
                {
                    head.Dispose();
                }
            }
        }
    }
}