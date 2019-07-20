using System;

namespace LogGrokCore.Data
{
    public class HeaderProvider
    {
        private Lazy<string?> _header;

        public HeaderProvider(
            LineIndex lineIndex,
            LogFile logFile)
        {
            _header = new Lazy<string?>(() => TryGetHeader(lineIndex, logFile, out var header) ? header : null);
        }

        public string? Header => _header.Value;

        private bool TryGetHeader(LineIndex lineIndex, LogFile logFile, out string? header)
        {
            if (lineIndex.Count == 0)
                throw new InvalidOperationException("Unable detect header before start file processing");
            var (offset, _) = lineIndex.GetLine(0);
            if (offset == 0)
            {
                header = null;
                return false;
            }

            var headerLength = (int)offset;
            using var stream = logFile.Open();
            var buffer = new byte[headerLength];
            stream.Read(buffer.AsSpan());
            header = logFile.Encoding.GetString(buffer).TrimEnd();
            return true;
        }
    }
}