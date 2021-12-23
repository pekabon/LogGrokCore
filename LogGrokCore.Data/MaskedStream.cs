using System;
using System.IO;

namespace LogGrokCore.Data
{
    class MaskedStream : Stream
    {
        private readonly Stream _stream;
        private readonly ulong _xorKey;

        internal MaskedStream(Stream originalStream, byte xorMask)
        {
            _stream = originalStream;
            ulong xorKey = 0;
            ulong mask = xorMask;
            for (var i = 0; i < 8; i++)
            {
                xorKey <<= 8;
                xorKey |= mask;
            }

            _xorKey = xorKey;
        }

        public override void Flush() => _stream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _stream.Read(buffer, offset, count);
            XorBuffer(buffer, offset, read, _xorKey);
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        private static void XorBuffer(byte[] buffer, int offset, int count, ulong key)
        {
            const int alignSize = sizeof(ulong);
            const int alignMask = alignSize - 1;
            unsafe
            {
                fixed (byte* dst = buffer)
                {
                    var ptr = dst + offset;
                    var end = ptr + count;

                    for (; ptr < end && ((uint)ptr & alignMask) != 0; ++ptr)
                        *ptr ^= (byte)key;

                    var alignedEnd = end - ((uint)end & alignMask);
                    for (; ptr < alignedEnd; ptr += alignSize)
                        *(ulong*)ptr ^= key;

                    for (; ptr < end; ++ptr)
                        *ptr ^= (byte)key;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}