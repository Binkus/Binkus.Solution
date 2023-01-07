using System.Reactive.Concurrency;

namespace Binkus.Extensions;

public static class SchedulerExt
{
#if NET5_0_OR_GREATER
    public static Task InvokeAsync(this IScheduler scheduler, Action action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.Schedule((taskCompletionSource, action, cancellationToken),
            static (state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.action();
                    state.taskCompletionSource.TrySetResult();
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    public static Task InvokeAsync(this IScheduler scheduler, TimeSpan dueTime, Action action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.Schedule((taskCompletionSource, action, cancellationToken), dueTime,
            static (state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.action();
                    state.taskCompletionSource.TrySetResult();
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    public static Task InvokeAsync(this IScheduler scheduler, DateTimeOffset dueTime, Action action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.Schedule((taskCompletionSource, action, cancellationToken), dueTime,
            static (state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.action();
                    state.taskCompletionSource.TrySetResult();
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
#endif
    
    //
    
    public static Task<TResult> InvokeAsync<TResult>(this IScheduler scheduler, Func<TResult> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<TResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.Schedule((taskCompletionSource, action, cancellationToken),
            static (state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.taskCompletionSource.TrySetResult(state.action());
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    public static Task<TResult> InvokeAsync<TResult>(this IScheduler scheduler, TimeSpan dueTime, Func<TResult> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<TResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.Schedule((taskCompletionSource, action, cancellationToken), dueTime,
            static (state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.taskCompletionSource.TrySetResult(state.action());
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    public static Task<TResult> InvokeAsync<TResult>(this IScheduler scheduler, DateTimeOffset dueTime, Func<TResult> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<TResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.Schedule((taskCompletionSource, action, cancellationToken), dueTime,
            static (state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.taskCompletionSource.TrySetResult(state.action());
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    // Async
    
#if NET5_0_OR_GREATER
    public static Task InvokeAsync(this IScheduler scheduler, Func<Task> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.ScheduleAsync((taskCompletionSource, action, cancellationToken),
            static async (_, state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    await state.action().ConfigureAwait(false);
                    state.taskCompletionSource.TrySetResult();
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    public static Task InvokeAsync(this IScheduler scheduler, TimeSpan dueTime, Func<Task> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.ScheduleAsync((taskCompletionSource, action, cancellationToken), dueTime,
            static async (_, state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    await state.action().ConfigureAwait(false);
                    state.taskCompletionSource.TrySetResult();
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    public static Task InvokeAsync(this IScheduler scheduler, DateTimeOffset dueTime, Func<Task> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.ScheduleAsync((taskCompletionSource, action, cancellationToken), dueTime,
            static async (_, state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    await state.action().ConfigureAwait(false);
                    state.taskCompletionSource.TrySetResult();
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
#endif
    
    //
    
    public static Task<TResult> InvokeAsync<TResult>(this IScheduler scheduler, Func<Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<TResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.ScheduleAsync((taskCompletionSource, action, cancellationToken),
            static async (_, state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.taskCompletionSource.TrySetResult(await state.action().ConfigureAwait(false));
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    public static Task<TResult> InvokeAsync<TResult>(this IScheduler scheduler, TimeSpan dueTime, Func<Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<TResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.ScheduleAsync((taskCompletionSource, action, cancellationToken), dueTime,
            static async (_, state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.taskCompletionSource.TrySetResult(await state.action().ConfigureAwait(false));
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    public static Task<TResult> InvokeAsync<TResult>(this IScheduler scheduler, DateTimeOffset dueTime, Func<Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<TResult> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        taskCompletionSource.TryRegisterCancellation(cancellationToken);
        if (cancellationToken.IsCancellationRequested) return taskCompletionSource.Task;
        var subscription = scheduler.ScheduleAsync((taskCompletionSource, action, cancellationToken), dueTime,
            static async (_, state, _) =>
            {
                if (state.cancellationToken.IsCancellationRequested)
                {
                    state.taskCompletionSource.TrySetCanceled(state.cancellationToken);
                    return;
                }
                try
                {
                    state.taskCompletionSource.TrySetResult(await state.action().ConfigureAwait(false));
                }
                catch (Exception e)
                {
                    state.taskCompletionSource.TrySetException(e);
                }
            });
        subscription.TryRegisterUnsubscribeOnCancellation(cancellationToken);
        return taskCompletionSource.Task;
    }
    
    // Helper

    private static void TryRegisterCancellation<TResult>(
        this TaskCompletionSource<TResult> taskCompletionSource, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled) return;
        if (cancellationToken.IsCancellationRequested)
        {
            taskCompletionSource.TrySetCanceled(cancellationToken);
            return;
        }
        cancellationToken.Register(
            static s =>
                (((TaskCompletionSource<TResult> taskCompletionSource, CancellationToken cancellationToken))s!)
                .taskCompletionSource
                .TrySetCanceled(
                    (((TaskCompletionSource<TResult> taskCompletionSource, CancellationToken cancellationToken))s)
                    .cancellationToken), (taskCompletionSource, cancellationToken));
    }

#if NET5_0_OR_GREATER
    private static void TryRegisterCancellation(
        this TaskCompletionSource taskCompletionSource, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled) return;
        if (cancellationToken.IsCancellationRequested)
        {
            taskCompletionSource.TrySetCanceled(cancellationToken);
            return;
        }
        cancellationToken.Register(
            static s =>
                (((TaskCompletionSource taskCompletionSource, CancellationToken cancellationToken))s!)
                .taskCompletionSource
                .TrySetCanceled(
                    (((TaskCompletionSource taskCompletionSource, CancellationToken cancellationToken))s)
                    .cancellationToken), (taskCompletionSource, cancellationToken));
    }
#endif
    
    private static void TryRegisterUnsubscribeOnCancellation(
        this IDisposable subscription, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled) return;
        if (cancellationToken.IsCancellationRequested)
        {
            subscription.Dispose();
            return;
        }
        cancellationToken.Register(static s => ((IDisposable)s!).Dispose(), subscription);
    }
}