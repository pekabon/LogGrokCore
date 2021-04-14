using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace LogGrokCore.Bootstrap
{
    public class SingleInstanceManager
    {
        private App? _app;
        private Mutex _singleInstanceMutex;
        private readonly bool _isFirstInstance;
        private const string SingleInstanceMutexName = "2A7759B1-AA14-4ABA-A05C-CFFEF9CE1D5A";
        private const string PipeName = "01DD1D8E-322A-4AA1-B9BB-E7B4A69C8986";
        private const string MessageSeparator = "|:|";
        
        public SingleInstanceManager()
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out _isFirstInstance);
        }

        public void Run(string[] args)
        {
            if (_isFirstInstance)
            {
                using CancellationTokenSource exitCancellationTokenSource = new();
                _app = new App();
                StartListeningNextInstances(argsCommandLine => 
                    _app.OnNextInstanceStared(argsCommandLine), exitCancellationTokenSource.Token);
                _app.Run();
                exitCancellationTokenSource.Cancel();
                return;
            }

            if (args.Length > 0)
                TransferArgumentsToFirstInstance(args);
        }

        private async void StartListeningNextInstances(Action<IEnumerable<string>> onNextInstanceStarted, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync(token);
                    using var reader = new StreamReader(server);
                    var message = await reader.ReadToEndAsync();
                    onNextInstanceStarted(message.Split(MessageSeparator));
                    server.Disconnect();                
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void TransferArgumentsToFirstInstance(string[] args)
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.ConnectAsync();
            using var writer = new StreamWriter(client);
            writer.Write(string.Join(MessageSeparator, args));
        }
    }
}
