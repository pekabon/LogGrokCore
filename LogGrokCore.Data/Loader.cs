using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{
    public class Loader : IDisposable
    {
        private readonly Task _loadingTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private const int BufferSize = 1024*1024;

        public Loader(LogMetaInformation metaInformation, 
            Func<Stream> streamFactory, 
            LineIndex lineIndex,
            ILineDataConsumer lineProcessor)
        {
            var encoding = metaInformation.Encoding;
            var loaderImpl = new LoaderImpl(BufferSize, lineIndex, lineProcessor);
            _cancellationTokenSource = new CancellationTokenSource();
            _loadingTask = Task.Factory.StartNew(
                () => loaderImpl.Load(streamFactory(), encoding.GetBytes("\r"), encoding.GetBytes("\n"),
                    _cancellationTokenSource.Token));
        }

        public bool IsLoading => !_loadingTask.IsCompleted;

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _loadingTask.Wait();
            _loadingTask?.Dispose();
        }
    }
}