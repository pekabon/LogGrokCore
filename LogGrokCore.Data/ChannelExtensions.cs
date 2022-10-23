using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LogGrokCore.Data;

public static class ChannelExtensions
{
    public static Task StartConsumers<T>(this Channel<T> channel,
        Func<ChannelReader<T>, Task> producerFactory, int consumerCount)
    {
        var aliveConsumers = consumerCount;
        var taskCompletionSource = new TaskCompletionSource(); 
        for (var i = 0; i < consumerCount; i++)
        {
            _ = Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        await producerFactory(channel.Reader);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref aliveConsumers) == 0)
                        {
                            taskCompletionSource.SetResult();
                        }
                    }
                });
        }

        return taskCompletionSource.Task;
    }
}