using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace LogGrokCore.Data.Search;

public class ValueTaskSource<TResult> : IValueTaskSource<TResult>
{
    private ManualResetValueTaskSourceCore<TResult> _source = new();
   
    public TResult GetResult(short token) => _source.GetResult(token);

    public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => 
        _source.OnCompleted(continuation, state, token, flags);

    public void SetResult(TResult result) => _source.SetResult(result);

    public void SetException(Exception exception) => _source.SetException(exception);

    public ValueTask<TResult> GetTask()
    {
        return new ValueTask<TResult>(this, 0);
    }
}