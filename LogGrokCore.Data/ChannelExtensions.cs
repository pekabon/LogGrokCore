using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LogGrokCore.Data;

public static class ChannelExtensions
{
    public static Task StartProducers<T>(this Channel<T> channel,
        Func<ChannelWriter<T>, Task> producerFactory, int producerCount)
    {
        var aliveConsumers = producerCount;
        var taskCompletionSource = new TaskCompletionSource(); 
        for (var i = 0; i < producerCount; i++)
        {
            _ = Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        await producerFactory(channel.Writer);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref aliveConsumers) == 0)
                        {
                            channel.Writer.Complete();
                            taskCompletionSource.SetResult();
                        }
                    }
                });
        }

        return taskCompletionSource.Task;
    }
}