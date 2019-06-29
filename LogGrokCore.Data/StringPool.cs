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
            var pooledStringSize = size < 32 ?  32 : Pow2Roundup(size);
           
            var bucket = _buckets.GetOrAdd(size, _bucketFactory(pooledStringSize ));
            return bucket.Rent();
        }

        public void Return(string returned)
        {
            if (!_buckets.TryGetValue(returned.Length, out var bucket)) 
                throw new InvalidOperationException();
            
            bucket.Return(returned);
        }
        
        private static int Pow2Roundup (int x)
        {
            if (x < 0)
                return 0;
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x+1;
        }
    }
}