using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{
    public sealed class Loader : IDisposable
    {
        private readonly Task _loadingTask;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private const int BufferSize = 4*1024*1024;
        private Channel<long> _logUpdatesChannel;
        public Loader(
            LogFile logFile,
            Func<ILineDataConsumer> lineProcessorFactory)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _logUpdatesChannel = Channel.CreateBounded<long>(
                new BoundedChannelOptions(1)
                {
                    FullMode = BoundedChannelFullMode.DropOldest
                });
            
            _loadingTask = Task.Factory.StartNew(
                async () =>
                {
                    var encoding = logFile.Encoding;
  
                    Trace.TraceInformation($"Start loading {logFile.FilePath}.");
                    var timeStamp = DateTime.Now;
                    var stream = logFile.OpenForSequentialRead();
           
                    var bytesRead = await LoadAsync(logFile.FilePath, stream, logFile.Encoding, lineProcessorFactory());
                    await StartWatching(logFile, lineProcessorFactory, bytesRead, _cancellationTokenSource.Token);
                });
        }
        
        private async Task<long> LoadAsync(
            string filePath, 
            Stream stream,
            Encoding encoding,
            ILineDataConsumer lineProcessor)
        {
            var loaderImpl = new LoaderImpl(BufferSize, lineProcessor);
            
            Trace.TraceInformation($"Start loading {filePath}.");
            var timeStamp = DateTime.Now;
            
            try
            {
                var result = await loaderImpl.Load(stream,
                    encoding.GetBytes("\r"), encoding.GetBytes("\n"),
                    _cancellationTokenSource.Token);
                Trace.TraceInformation($"Loaded {filePath}, time spent: {DateTime.Now - timeStamp}.");
                return result;
            }
            catch (OperationCanceledException)
            {
                Trace.TraceInformation($"Loading of {filePath} was cancelled.");
            }
            catch (Exception e)
            {
                Trace.TraceError(
                    $"Unexpected loading result while loading {filePath}. Exception: \r\n{e}");
            }

            return 0;
        }

        public IAsyncEnumerable<long> LogUpdates => _logUpdatesChannel.Reader.ReadAllAsync(); 

        private async Task StartWatching(LogFile logFile, Func<ILineDataConsumer> lineProcessorFactory, long currentSize,
            CancellationToken cancellationToken)
        {
            Trace.TraceInformation("Start watching for file changes");

            while (!cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine($"Start waiting {logFile.FilePath} to grow");
                var newSize = await WaitFileSizeToGrow(logFile.FilePath, currentSize);
                Debug.WriteLine($"File is grown, new size = {newSize}");
                if (newSize > currentSize && !cancellationToken.IsCancellationRequested)
                {
                    var stream = logFile.OpenForSequentialRead();
                    stream.Position = currentSize;
                    var timeStamp = DateTime.Now;
                    
                    currentSize = await LoadAsync(logFile.FilePath, stream, logFile.Encoding, lineProcessorFactory());
                    await _logUpdatesChannel.Writer.WriteAsync(currentSize, cancellationToken);
                    var timeDiff = DateTime.Now - timeStamp;
                    if (timeDiff.TotalMilliseconds < 300)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(300) - timeDiff, cancellationToken);
                    }
                }
            }
            _logUpdatesChannel.Writer.Complete();
        }

        private static async ValueTask<long> WaitFileSizeToGrow(string filePath, long currentSize)
        {            
            var waitFileToChangeCompletionSource = new TaskCompletionSource();
            var fileSystemWatcher = new FileSystemWatcher(
                Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetFileName(filePath)) { NotifyFilter = NotifyFilters.Size };
            fileSystemWatcher.Changed += (o, e) => waitFileToChangeCompletionSource.SetResult();

            var newSize = new FileInfo(filePath).Length;
            while (newSize <= currentSize)
            {
                await Task.WhenAny(waitFileToChangeCompletionSource.Task, Task.Delay(3000));
                newSize = new FileInfo(filePath).Length;
            }

            return newSize;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _loadingTask.Wait();
            _loadingTask.Dispose();
        }
    }
}