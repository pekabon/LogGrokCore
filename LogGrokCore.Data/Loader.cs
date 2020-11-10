using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LogGrokCore.Data
{
    public class Loader : IDisposable
    {
        private readonly Task _loadingTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private ILogger _logger;
        private const int BufferSize = 1024*1024;

        public Loader(
            LogFile logFile,
            ILineDataConsumer lineProcessor,
            ILogger logger)
        {
            _logger = logger;
            var encoding = logFile.Encoding;
            var loaderImpl = new LoaderImpl(BufferSize, lineProcessor);
            _cancellationTokenSource = new CancellationTokenSource();
            
            logger.LogInformation($"Start loading {logFile.FilePath}.");
            var timeStamp = DateTime.Now;
            _loadingTask = Task.Factory.StartNew(
                () => loaderImpl.Load(logFile.OpenForSequentialRead(), 
                    encoding.GetBytes("\r"), encoding.GetBytes("\n"),
                    _cancellationTokenSource.Token))
                .ContinueWith(t =>
                {
                    switch(t.Status)
                    {
                        case TaskStatus.RanToCompletion: 
                            logger.LogInformation($"Loaded {logFile.FilePath}, time spent: {DateTime.Now - timeStamp}.");
                            
                            break; 
                        case TaskStatus.Canceled: logger.LogInformation($"Loading of {logFile.FilePath} was cancelled.");
                            break;
                        default: logger.LogError($"Unexpected loading result {t.Status} while loading {logFile.FilePath}.");
                            break;
                    }
                });
        }

        public bool IsLoading => !_loadingTask.IsCompleted;

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _loadingTask.Wait();
            _loadingTask.Dispose();
        }
    }
}