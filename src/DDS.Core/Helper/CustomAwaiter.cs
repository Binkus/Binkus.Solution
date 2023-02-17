using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Threading;

namespace DDS.Core.Helper;

[SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
public static class CustomAwaiterExtensions
{
    public static TaskAwaiter GetAwaiter(this TaskAwaiter taskAwaiter) => taskAwaiter;
    public static TaskAwaiter<T> GetAwaiter<T>(this TaskAwaiter<T> taskAwaiter) => taskAwaiter;

    
    public static ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter(
        this ConfiguredTaskAwaitable.ConfiguredTaskAwaiter taskAwaiter) => taskAwaiter;
    
    public static ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter GetAwaiter<T>(
        this ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter taskAwaiter) => taskAwaiter;
    
    public static ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter GetAwaiter(
        this ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter taskAwaiter) => taskAwaiter;
    
    public static ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter GetAwaiter<T>(
        this ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter taskAwaiter) => taskAwaiter;
    
    public static void GetResult(this ConfiguredTaskAwaitable taskAwaiter) => taskAwaiter.GetAwaiter().GetResult();
    public static T GetResult<T>(this ConfiguredTaskAwaitable<T> taskAwaiter) => taskAwaiter.GetAwaiter().GetResult();
    
    public static void GetResult(this ConfiguredValueTaskAwaitable taskAwaiter) => taskAwaiter.GetAwaiter().GetResult();
    public static T GetResult<T>(this ConfiguredValueTaskAwaitable<T> taskAwaiter) => taskAwaiter.GetAwaiter().GetResult();
    
    // public static ConfiguredTaskAwaitable GetAwaiter(this ConfiguredTaskAwaitable taskAwaiter) => taskAwaiter;

    //
    
    public static void AwaitSync(this TaskAwaiter taskAwaiter) => taskAwaiter.GetResult();
    public static T AwaitSync<T>(this TaskAwaiter<T> taskAwaiter) => taskAwaiter.GetResult();
    
    public static void AwaitSync(this ValueTaskAwaiter taskAwaiter) => taskAwaiter.GetResult();
    public static T AwaitSync<T>(this ValueTaskAwaiter<T> taskAwaiter) => taskAwaiter.GetResult();
    
    public static void AwaitSync(this ConfiguredValueTaskAwaitable taskAwaiter) => taskAwaiter.GetResult();
    public static T AwaitSync<T>(this ConfiguredValueTaskAwaitable<T> taskAwaiter) => taskAwaiter.GetResult();
    
    public static void AwaitSync(this ConfiguredTaskAwaitable taskAwaiter) => taskAwaiter.GetResult();
    public static T AwaitSync<T>(this ConfiguredTaskAwaitable<T> taskAwaiter) => taskAwaiter.GetResult();
    
    //
    
    public static void AwaitSync(this ConfiguredTaskAwaitable.ConfiguredTaskAwaiter taskAwaiter)
        => taskAwaiter.GetResult();
    
    public static T AwaitSync<T>(this ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter taskAwaiter)
        => taskAwaiter.GetResult();
    
    public static void AwaitSync(this ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter taskAwaiter)
        => taskAwaiter.GetResult();
    
    public static T AwaitSync<T>(this ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter taskAwaiter)
        => taskAwaiter.GetResult();
    
    //
    
    public static void AwaitSync(this Task task) => task.GetAwaiter().GetResult();
    public static T AwaitSync<T>(this Task<T> task) => task.GetAwaiter().GetResult();
    
    public static void AwaitSync(this ValueTask task) => task.GetAwaiter().GetResult();
    public static T AwaitSync<T>(this ValueTask<T> task) => task.GetAwaiter().GetResult();
    
    //
    
    public static void AwaitSync(this JoinableTask task, CancellationToken cancellationToken = default)
        => task.Join(cancellationToken);
    public static T AwaitSync<T>(this JoinableTask<T> task, CancellationToken cancellationToken = default)
        => task.Join(cancellationToken);
    
    //
    
    //
    
    public static void AwaitOn(this TaskAwaiter taskAwaiter, IScheduler scheduler) => taskAwaiter.GetResult();
    public static T AwaitOn<T>(this TaskAwaiter<T> taskAwaiter, IScheduler scheduler) => taskAwaiter.GetResult();
    
    public static void AwaitOn(this ValueTaskAwaiter taskAwaiter, IScheduler scheduler) => taskAwaiter.GetResult();
    public static T AwaitOn<T>(this ValueTaskAwaiter<T> taskAwaiter, IScheduler scheduler) => taskAwaiter.GetResult();
    
    public static void AwaitOn(this ConfiguredValueTaskAwaitable taskAwaiter, IScheduler scheduler) => taskAwaiter.GetResult();
    public static T AwaitOn<T>(this ConfiguredValueTaskAwaitable<T> taskAwaiter, IScheduler scheduler) => taskAwaiter.GetResult();
    
    public static void AwaitOn(this ConfiguredTaskAwaitable taskAwaiter, IScheduler scheduler) => taskAwaiter.GetResult();
    public static T AwaitOn<T>(this ConfiguredTaskAwaitable<T> taskAwaiter, IScheduler scheduler) => taskAwaiter.GetResult();
    
    //
    
    public static void AwaitOn(this ConfiguredTaskAwaitable.ConfiguredTaskAwaiter taskAwaiter, IScheduler scheduler)
        => taskAwaiter.GetResult();
    
    public static T AwaitOn<T>(this ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter taskAwaiter, IScheduler scheduler)
        => taskAwaiter.GetResult();
    
    public static void AwaitOn(this ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter taskAwaiter, IScheduler scheduler)
        => taskAwaiter.GetResult();
    
    public static T AwaitOn<T>(this ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter taskAwaiter, IScheduler scheduler)
        => taskAwaiter.GetResult();
    
    //
    
    public static void AwaitOn(this Task task, IScheduler scheduler) => task.GetAwaiter().GetResult();
    public static T AwaitOn<T>(this Task<T> task, IScheduler scheduler) => task.GetAwaiter().GetResult();
    
    public static void AwaitOn(this ValueTask task, IScheduler scheduler) => task.GetAwaiter().GetResult();
    public static T AwaitOn<T>(this ValueTask<T> task, IScheduler scheduler) => task.GetAwaiter().GetResult();
    
    //
    
    public static void AwaitOn(this JoinableTask task, IScheduler scheduler, CancellationToken cancellationToken = default)
        => task.Join(cancellationToken);
    public static T AwaitOn<T>(this JoinableTask<T> task, IScheduler scheduler, CancellationToken cancellationToken = default)
        => task.Join(cancellationToken);
    
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