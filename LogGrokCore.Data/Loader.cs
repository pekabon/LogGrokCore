using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{

    public class LineParser : ILineDataConsumer
    {
        public void AddLineData(uint lineNumber, Span<byte> lineData)
        {
        }
    }

    public class Loader
    {
        private readonly LineIndex _lineIndex;
        private readonly Task _loadingTask;
        private const int BufferSize = 1024*1024;

        public Loader(Func<Stream> streamFactory)
        {
            var encoding = DetectEncoding(streamFactory());
            _lineIndex = new LineIndex();
            var lineParser = new LineParser();
            var loaderImpl = new LoaderImpl(BufferSize, _lineIndex, lineParser);
            _loadingTask = Task.Factory.StartNew(() => loaderImpl.Load(streamFactory(), encoding.GetBytes("\r"), encoding.GetBytes("\n")));

            LineProvider = new LineProvider(_lineIndex, streamFactory, encoding);
        }

        public LineProvider LineProvider { get; }

        public bool IsLoading => !_loadingTask.IsCompleted;

        private Encoding DetectEncoding(Stream stream)
        {
            using var reader = new StreamReader(stream);
            _ = reader.ReadLine();
            return reader.CurrentEncoding;
        }
    }
}