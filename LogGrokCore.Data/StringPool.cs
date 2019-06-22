using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{

    public class StringPool
    {
        private class StringPoolBucket
        {
            private readonly int _stringSize;
            private readonly ConcurrentBag<string> _pool = new ConcurrentBag<string>();
            public StringPoolBucket(int stringSize)
            {
                _stringSize = stringSize;
            }
            public string Rent()
            {
                if (_pool.TryTake(out var result))
                    return result;

                return new string('\0', _stringSize);
            }

            public void Return(string returned)
            {
                _pool.Add(returned);
            }
        }

        ConcurrentDictionary<int, StringPoolBucket> _buckets = new ConcurrentDictionary<int, StringPoolBucket>();

        private Func<int, StringPoolBucket> _bucketFactory = size => new StringPoolBucket(size);
        public string Rent(int size)
        {
            var bucket = _buckets.GetOrAdd(size, _bucketFactory(size));
            return bucket.Rent();
        }

        public void Return(string returned)
        {
            if (_buckets.TryGetValue(returned.Length, out var bucket))
            {
                bucket.Return(returned);
            }
            throw new InvalidOperationException();
        }
    }
}