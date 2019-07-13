using System;
using System.Collections.Generic;
using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data.Index
{
    public class IndexKey : IEquatable<IndexKey>
    {
        private string _buffer;
        private int _start;
        private readonly int _componentCount;

        public IndexKey(string buffer, int start, int componentCount)
        {
            _buffer = buffer;
            _start = start;
            _componentCount = componentCount;
        }

        public unsafe void MakeCopy()
        {
            var bufferSpan = _buffer.AsSpan(_start);
            fixed (char* start = bufferSpan)
            {
                var meta =  LineMetaInformation.Get(start, _componentCount);
                var size = meta.TotalSizeWithPayloadCharsAligned;
                _buffer = new string(start, 0, size);
                _start = 0;
            }
        }

        public bool Equals(IndexKey? other)
        {
            if (other == null)
                return false;
            
            var dataSpan = GetDataSpan();
            var meta = GetComponentsMeta();

            var otherDataSpan = other.GetDataSpan();
            var otherMeta = other.GetComponentsMeta();

            for (var idx = 0; idx < _componentCount; idx++)
            {
                if (!meta.GetComponent(dataSpan, idx).SequenceEqual(otherMeta.GetComponent(otherDataSpan, idx)))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is IndexKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var meta = GetComponentsMeta();
                var dataSpan = GetDataSpan();
                var result = 17;

                for (var i = 0; i < _componentCount; i++)
                {
                    foreach (var ch in meta.GetComponent(dataSpan, i))
                    {
                        result = result * 31 + ch.GetHashCode();
                    }
                }

                return result;
            }
        }

        public override string ToString()
        {
            var meta = GetComponentsMeta();
            var dataSpan = GetDataSpan();

            var strings = new List<string>(_componentCount);
            for (var i = 0; i < _componentCount; i++)
            {
                strings.Add(meta.GetComponent(dataSpan, i).ToString());
            }

            return $"{{{string.Join(',', strings)}}}";
        }

        private unsafe ParsedLineComponents GetComponentsMeta()
        {
            var bufferSpan = _buffer.AsSpan(_start);
            fixed (char* start = bufferSpan)
            {
                return LineMetaInformation.Get(start, _componentCount).ParsedLineComponents;
            }
        }

        private ReadOnlySpan<char> GetDataSpan()
        {
            var dataSpan = _buffer.AsSpan(_start + LineMetaInformation.GetSizeChars(_componentCount));
            return dataSpan;
        }
    }
}