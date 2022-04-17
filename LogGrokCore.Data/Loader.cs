using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LogGrokCore.Data
{
    public class Loader : IDisposable
    {
        private readonly Task _loadingTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private const int BufferSize = 1024*1024;

        public Loader(
            LogFile logFile,
            ILineDataConsumer lineProcessor,
            ILogger logger)
        {
            var encoding = logFile.Encoding;
            var loaderImpl = new LoaderImpl(BufferSize, lineProcessor);
            _cancellationTokenSource = new CancellationTokenSource();
            
            Trace.TraceInformation($"Start loading {logFile.FilePath}.");
            var timeStamp = DateTime.Now;
            _loadingTask = Task.Factory.StartNew(

                async () =>
                {
                    try
                    {
                        await loaderImpl.Load(logFile.OpenForSequentialRead(),
                            encoding.GetBytes("\r"), encoding.GetBytes("\n"),
                            _cancellationTokenSource.Token);
                        Trace.TraceInformation($"Loaded {logFile.FilePath}, time spent: {DateTime.Now - timeStamp}.");
                    }
                    catch (OperationCanceledException)
                    {
                        Trace.TraceInformation($"Loading of {logFile.FilePath} was cancelled.");
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError(
                            $"Unexpected loading result while loading {logFile.FilePath}. Exception: \r\n{e}");
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