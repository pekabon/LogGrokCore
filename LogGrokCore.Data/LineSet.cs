using System;

namespace LogGrokCore.Data
{
    public struct LineSet 
    {
        public class LineSetEnumerator
        {
            private int _position = -1;
            private readonly int _componentCount;
            private readonly string _stringBuffer;

            public LineSetEnumerator(string stringBuffer, int componentCount)
            {
                _componentCount = componentCount;
                _stringBuffer = stringBuffer;
            }

            public unsafe LineMetaInformation Current
            {
                get
                {
                    fixed (char* pointer = _stringBuffer.AsSpan(_position))
                    {
                        return LineMetaInformation.Get(pointer, _componentCount);
                    }
                }
            }
            public void Dispose()
            {
            }

            public unsafe bool MoveNext()
            {
                if (_position < 0)
                {
                    _position = 0;
                    return true;
                }

                fixed (char* pointer = _stringBuffer.AsSpan(_position))
                {
                    var node = LineMetaInformationNode.Get(pointer, _componentCount);
                    var offset = node.NextNodeOffset;
                    if (offset <= 0) return false;
                    _position = offset;
                    return true;
                }
            }

            public void Reset()
            {
                _position = -1;
            }
        }

        private readonly string _stringBuffer;
        private readonly int _componentCount;

        public LineSet(string stringBuffer, int componentCount)
        {
            _stringBuffer = stringBuffer;
            _componentCount = componentCount;
        }

        public LineSetEnumerator GetEnumerator()
        {
            return new LineSetEnumerator(_stringBuffer, _componentCount);
        }
    }
}