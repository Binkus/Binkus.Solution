using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.Threading;

namespace DDS.Core.Helper;

public static class TaskExtensions
{
    public static async Task<ExceptionDispatchInfo?> TryAwaitAsync(this Task task, bool logToDebug = false)
    {
        try
        {
            await task;
            return null;
        }
        catch (Exception e)
        {
            if (logToDebug) Debug.WriteLine(e);
            return ExceptionDispatchInfo.Capture(e);
        }
    }
    
    public static async Task<ExceptionDispatchInfo?> TryAwaitAsync(this JoinableTask task, bool logToDebug = false)
    {
        try
        {
            await task;
            return null;
        }
        catch (Exception e)
        {
            if (logToDebug) Debug.WriteLine(e);
            return ExceptionDispatchInfo.Capture(e);
        }
    }
    
    public static async Task<ExceptionDispatchInfo?> TryAwaitAsync<TException>(this Task task, bool logToDebug = false)
        where TException : Exception
    {
        try
        {
            await task;
            return null;
        }
        catch (TException e)
        {
            if (logToDebug) Debug.WriteLine(e);
            return ExceptionDispatchInfo.Capture(e);
        }
    }
    
    public static async Task<ExceptionDispatchInfo?> TryAwaitAsync<TException>(this JoinableTask task, bool logToDebug = false)
        where TException : Exception
    {
        try
        {
            await task;
            return null;
        }
        catch (TException e)
        {
            if (logToDebug) Debug.WriteLine(e);
            return ExceptionDispatchInfo.Capture(e);
        }
    }
    
    //
    
    public static async Task IgnoreExceptionAsync<TException>(this Task task, bool logToDebug = false)
        where TException : Exception
    {
        try
        {
            await task;
        }
        catch (TException e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
    }
    
    public static async Task IgnoreExceptionAsync<TException>(this JoinableTask task, bool logToDebug = false)
        where TException : Exception
    {
        try
        {
            await task;
        }
        catch (TException e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
    }
    
    public static async Task IgnoreExceptionAsync<TException1, TException2>(this Task task, bool logToDebug = false) 
        where TException1 : Exception where TException2 : Exception
    {
        try
        {
            await task;
        }
        catch (TException1 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
        catch (TException2 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
    }
    
    public static async Task IgnoreExceptionAsync<TException1, TException2>(this JoinableTask task, bool logToDebug = false) 
        where TException1 : Exception where TException2 : Exception
    {
        try
        {
            await task;
        }
        catch (TException1 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
        catch (TException2 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
    }
    
    public static async Task IgnoreExceptionAsync<TException1, TException2, TException3>(this Task task, bool logToDebug = false) 
        where TException1 : Exception where TException2 : Exception where TException3 : Exception
    {
        try
        {
            await task;
        }
        catch (TException1 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
        catch (TException2 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
        catch (TException3 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
    }
    
    public static async Task IgnoreExceptionAsync<TException1, TException2, TException3>(this JoinableTask task, bool logToDebug = false) 
        where TException1 : Exception where TException2 : Exception where TException3 : Exception
    {
        try
        {
            await task;
        }
        catch (TException1 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
        catch (TException2 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
        catch (TException3 e)
        {
            if (logToDebug) Debug.WriteLine(e);
        }
    }
    
    //
    
    public static async Task AwaitBeforeExecution(this Task task, params Task[] tasks)
    {
        foreach (var t in tasks) await t;
        await task;
    }
    
    public static async ValueTask YieldAndContinue(this ValueTask task)
    {
        await Task.Yield();
        await task;
    }
    
    public static async ValueTask<T> YieldAndContinue<T>(this ValueTask<T> task)
    {
        await Task.Yield();
        return await task;
    }
    
    public static async Task YieldAndContinue(this Task task)
    {
        await Task.Yield();
        await task;
    }
    
    public static async Task<T> YieldAndContinue<T>(this Task<T> task)
    {
        await Task.Yield();
        return await task;
    }
    
    //
    
    public static async ValueTask<T> Delay<T>(this ValueTask<T> task, TimeSpan delay)
    {
        await Task.Delay(delay);
        return await task;
    }
    
    public static async ValueTask Delay(this ValueTask task, TimeSpan delay)
    {
        await Task.Delay(delay);
        await task;
    }
    
    public static async Task<T> Delay<T>(this Task<T> task, TimeSpan delay)
    {
        await Task.Delay(delay);
        return await task;
    }
    
    public static async Task Delay(this Task task, TimeSpan delay)
    {
        await Task.Delay(delay);
        await task;
    }
    
    //
    
    public static async Task Try(this ValueTask task)
    {
        try { await task; }
        catch (Exception) { /* ignore */ }
    }
    
    public static async Task<T?> Try<T>(this ValueTask<T> task)
    {
        try { return await task; }
        catch (Exception) { return default; }
    }
    
    public static async Task Try(this Task task)
    {
        try { await task; }
        catch (Exception) { /* ignore */ }
    }
    
    public static async Task<T?> Try<T>(this Task<T> task)
    {
        try { return await task; }
        catch (Exception) { return default; }
    }
    
    //
    
    public static void AwaitSync(this Task task) => task.GetAwaiter().GetResult();
    public static T AwaitSync<T>(this Task<T> task) => task.GetAwaiter().GetResult();

    public static void AwaitSync(this ValueTask task) => task.GetAwaiter().GetResult();
    public static T AwaitSync<T>(this ValueTask<T> task) => task.GetAwaiter().GetResult();
    
    //
    
    public static void TryAwaitSync(this ValueTask task)
    {
        try { task.GetAwaiter().GetResult(); }
        catch (Exception) { /* ignore */ }
    }

    public static T? TryAwaitSync<T>(this ValueTask<T> task)
    {
        try { return task.GetAwaiter().GetResult(); }
        catch (Exception) { return default; }
    }
    
    public static void TryAwaitSync(this Task task)
    {
        try { task.GetAwaiter().GetResult(); }
        catch (Exception) { /* ignore */ }
    }

    public static T? TryAwaitSync<T>(this Task<T> task)
    {
        try { return task.GetAwaiter().GetResult(); }
        catch (Exception) { return default; }
    }
    
    //

    private static bool ContinueWithWhenTrue(this bool b, Action? continueWith)
    {
        if (b) continueWith?.Invoke();
        return b;
    }
    
    public static bool TrySetResultsToSource(this Task task, TaskCompletionSource s, CancellationToken? cancellationToken = default, bool sourceTaskToCompletionState = true, Action? continueWith = default)
    {
        if (task.IsInAnyRunningState()) return false;
        
        if (task.Exception is not null)
        {
            return s.TrySetException(task.Exception.InnerException ?? task.Exception).ContinueWithWhenTrue(continueWith);
        }
        if (cancellationToken is { IsCancellationRequested: true })
        {
            return s.TrySetCanceled(cancellationToken.Value).ContinueWithWhenTrue(continueWith);
        }
        if (task.IsCanceled)
        {
            return s.TrySetCanceled().ContinueWithWhenTrue(continueWith);
        }
        return sourceTaskToCompletionState && s.TrySetResult().ContinueWithWhenTrue(continueWith);
    }
    
    public static bool TrySetResultsToSource<T>(this Task<T> task, TaskCompletionSource<T> s, CancellationToken? cancellationToken = default, bool sourceTaskToCompletionState = true, Action? continueWith = default)
    {
        if (task.IsInAnyRunningState()) return false;
        
        if (task.Exception is not null)
        {
            return s.TrySetException(task.Exception.InnerException ?? task.Exception).ContinueWithWhenTrue(continueWith);
        }
        if (cancellationToken is { IsCancellationRequested: true })
        {
            return s.TrySetCanceled(cancellationToken.Value).ContinueWithWhenTrue(continueWith);
        }
        if (task.IsCanceled)
        {
            return s.TrySetCanceled().ContinueWithWhenTrue(continueWith);
        }

        // JoinableTaskFactory.MainThreadAwaiter
        // JoinableTaskFactory.Run
        // Synchronously waiting on tasks or awaiters may cause deadlocks. Use await or JoinableTaskFactory.Run instead.
        return sourceTaskToCompletionState && s.TrySetResult(task.Result).ContinueWithWhenTrue(continueWith);
    }
    
    //
    
    public static bool IsInFinalState(this Task task) => !task.IsInAnyRunningState();
    public static bool IsInAnyRunningState(this Task task)
    {
        // return task.Status.Equals(TaskStatus.WaitingForActivation) || task.Status.Equals(TaskStatus.Running);
        switch (task.Status)
        {
            case TaskStatus.Canceled:
            case TaskStatus.Faulted:
            case TaskStatus.RanToCompletion:
                return false;
            case TaskStatus.Created:
            case TaskStatus.Running:
            case TaskStatus.WaitingForActivation:
            case TaskStatus.WaitingForChildrenToComplete:
            case TaskStatus.WaitingToRun:
                return true;
            default:
                throw new UnreachableException($"Impossible invalid TaskStatus of {task.Status}.");
        }
    }
}