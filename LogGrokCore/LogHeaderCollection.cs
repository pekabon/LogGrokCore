using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public class LogHeaderCollection : IReadOnlyList<ItemViewModel>
    {
        private readonly LineIndex _lineIndex;
        private readonly LogFile _logFile;

        public LogHeaderCollection(
            LineIndex lineIndex,
            LogFile logFile)
        {
            _lineIndex = lineIndex;
            _logFile = logFile;
        }

        public IEnumerator<ItemViewModel> GetEnumerator()
        {
            return Source.Cast<ItemViewModel>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => Source.Length;

        public ItemViewModel this[int index] => Source[index];

        private ItemViewModel[] Source
        {
            get
            {
                _source ??= TryGetHeader();
                return _source ?? Array.Empty<ItemViewModel>();
            }
        }

        private ItemViewModel[]? _source; 
            
        private ItemViewModel[]? TryGetHeader()
        {
            if (_lineIndex.Count == 0)
                return null;
            
            var (offset, _) = _lineIndex.GetLine(0);
            if (offset == 0)
            {
                return Array.Empty<ItemViewModel>();
            }

            var headerLength = (int)offset;
            using var stream = _logFile.Open();
            var buffer = new byte[headerLength];
            stream.Read(buffer.AsSpan());
            var headerString = _logFile.Encoding.GetString(buffer).TrimEnd();
            return new ItemViewModel[] {new LogHeaderViewModel(headerString)};
        }
    }
}