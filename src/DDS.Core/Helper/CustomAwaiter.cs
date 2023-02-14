using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Threading;

namespace DDS.Core.Helper;

[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
public static class CustomAwaiterExtensions
{
    
}

public interface IAwaitable<out TAwaiter> where TAwaiter : ICriticalNotifyCompletion, INotifyCompletion
{
    TAwaiter GetAwaiter();
}

//

public interface ICustomAwaitable<out TAwaiter> : IAwaitable<TAwaiter>
    where TAwaiter : ICustomAwaiter, ICriticalNotifyCompletion, INotifyCompletion
{
    TAwaiter IAwaitable<TAwaiter>.GetAwaiter() => GetAwaiter();
    new TAwaiter GetAwaiter();
}

//

// public interface ICustomAwaitable<out TAwaiter, out TResult> : IAwaitable<TAwaiter>
//     where TAwaiter : ICustomAwaiter<TResult>, ICriticalNotifyCompletion, INotifyCompletion
// {
//     TAwaiter IAwaitable<TAwaiter>.GetAwaiter() => GetAwaiter();
//     new TAwaiter GetAwaiter();
// }
//
// public interface ICustomAwaitable2<out TResult> : IAwaitable<ICustomAwaiter<TResult>>
// {
//     ICustomAwaiter<TResult> IAwaitable<ICustomAwaiter<TResult>>.GetAwaiter() => GetAwaiter();
//     new ICustomAwaiter<TResult> GetAwaiter();
// }
//
// public interface ICustomAwaitable2 : IAwaitable<ICustomAwaiter>
// {
//     ICustomAwaiter IAwaitable<ICustomAwaiter>.GetAwaiter() => GetAwaiter();
//     new ICustomAwaiter GetAwaiter();
// }

//

public interface ICustomAwaiter : ICriticalNotifyCompletion
{
    bool IsCompleted { get; }
    
    void GetResult();
}

public interface ICustomAwaiter<out TResult> : ICustomAwaiter
{
    void ICustomAwaiter.GetResult() => GetResult();

    new TResult GetResult();
}

[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
public class CustomDelegateTaskAwaiterBase<T>
    where T : Task
{
    private readonly T _t;
    
    public CustomDelegateTaskAwaiterBase(T t) => _t = t;

    public bool IsCompleted => _t.IsCompleted;
    public void GetResult() => _t.GetAwaiter().GetResult();

    public void OnCompleted(Action continuation) => _t.ContinueWith(task => continuation, TaskScheduler.Current);

    public void UnsafeOnCompleted(Action continuation) => _t.ContinueWith(task => continuation, TaskScheduler.Current);
}

[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
public readonly struct CustomDelegateTaskAwaiter : ICustomAwaiter
{
    private readonly Task _task;
    
    public CustomDelegateTaskAwaiter(Task task) => _task = task;

    public bool IsCompleted => _task.IsCompleted;
    public void GetResult() => _task.GetAwaiter().GetResult();

    public void OnCompleted(Action continuation) => _task.ContinueWith(task => continuation, TaskScheduler.Current);

    public void UnsafeOnCompleted(Action continuation) => _task.ContinueWith(task => continuation, TaskScheduler.Current);
}

[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
public readonly struct CustomDelegateTaskAwaiter<TResult> : ICustomAwaiter<TResult>
{
    private readonly Task<TResult> _task;
    
    public CustomDelegateTaskAwaiter(Task<TResult> task) => _task = task;

    public bool IsCompleted => _task.IsCompleted;
    public TResult GetResult() => _task.GetAwaiter().GetResult();

    public void OnCompleted(Action continuation) => _task.ContinueWith(task => continuation, TaskScheduler.Current);

    public void UnsafeOnCompleted(Action continuation) => _task.ContinueWith(task => continuation, TaskScheduler.Current);
}

//

[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
public readonly struct CustomDelegateAwaiter : ICustomAwaiter
{
    private readonly ICriticalNotifyCompletion _delegate;
    private readonly Action? _continuationBeforeOnCompleted;
    private readonly Action? _continuationBeforeContinuation;
    private readonly Action? _continuationAfterContinuation;
    
    public CustomDelegateAwaiter(ICriticalNotifyCompletion customDelegate, 
        Action? continuationBeforeOnCompleted = default,
        Action? continuationBeforeContinuation = default,
        Action? continuationAfterContinuation = default)
    {
        _delegate = customDelegate;
        _continuationBeforeOnCompleted = continuationBeforeOnCompleted;
        _continuationBeforeContinuation = continuationBeforeContinuation;
        _continuationAfterContinuation = continuationAfterContinuation;
    }

    public bool IsCompleted =>
        _delegate switch
        {
            ICustomAwaiter awaiter => awaiter.IsCompleted,
            TaskAwaiter awaiter => awaiter.IsCompleted,
            ValueTaskAwaiter awaiter => awaiter.IsCompleted,
            _ => throw new NullReferenceException()
        };

    public void GetResult()
    {
        switch (_delegate)
        {
            case ICustomAwaiter awaiter:
                awaiter.GetResult();
                break;
            case TaskAwaiter awaiter:
                awaiter.GetResult();
                break;
            case ValueTaskAwaiter awaiter:
                awaiter.GetResult();
                break;
        }
    }
    
    public void OnCompleted(Action continuation)
    {
        _continuationBeforeOnCompleted?.Invoke();
        var before = _continuationBeforeContinuation;
        var after = _continuationAfterContinuation; 
        _delegate.OnCompleted(() =>
        {
            before?.Invoke();
            continuation();
            after?.Invoke();
        });
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        _continuationBeforeOnCompleted?.Invoke();
        var before = _continuationBeforeContinuation;
        var after = _continuationAfterContinuation; 
        _delegate.UnsafeOnCompleted(() =>
        {
            before?.Invoke();
            continuation();
            after?.Invoke();
        });
    }
}

public readonly struct CustomDelegateAwaiter2 : ICustomAwaiter
{
    private readonly ICustomAwaiter _delegate;
    
    public CustomDelegateAwaiter2(ICustomAwaiter @delegate) => _delegate = @delegate;

    public bool IsCompleted => _delegate.IsCompleted;
    public void GetResult() => _delegate.GetResult();

    public void OnCompleted(Action continuation) => _delegate.OnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation) => _delegate.UnsafeOnCompleted(continuation);
}

public readonly struct CustomDelegateAwaiter<TResult> : ICustomAwaiter<TResult>
{
    private readonly ICustomAwaiter<TResult> _delegate;
    
    public CustomDelegateAwaiter(ICustomAwaiter<TResult> @delegate) => _delegate = @delegate;

    public bool IsCompleted => _delegate.IsCompleted;
    public TResult GetResult() => _delegate.GetResult();

    public void OnCompleted(Action continuation) => _delegate.OnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation) => _delegate.UnsafeOnCompleted(continuation);
}