using System.Runtime.CompilerServices;

namespace DDS.Core.Helper;

public static class ProcessExtensions
{
    public static TaskAwaiter<int> GetAwaiter(this Process process) => process.ToTask().GetAwaiter();
    
    public static Task<int> ToTask(this Process process)
    {
        var tcs = new TaskCompletionSource<int>();
        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) => tcs.TrySetResult(process.ExitCode);
        if (process.HasExited) tcs.TrySetResult(process.ExitCode);
        return tcs.Task;
    }
    
    // ReSharper disable once CognitiveComplexity
    public static Task<int> ToCancelableTask(this Process process, 
        CancellationToken? cancellationToken = default, bool killEntireProcessTreeWhenCancelled = false,
        bool throwTaskCancelledException = false)
        => Task.Run(async () =>
        {
            await Task.Yield();
            var tcs = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            Task? cancelTask = null;
            if (cancellationToken.HasValue)
            {
                // var observable = cancellationToken.Value.SubscribeToExpressionChain<CancellationToken,bool>((CancellationToken t) => t.IsCancellationRequested);
                // var disposable = observable.Subscribe(o =>
                // {
                //     if (!o.Value) return;
                //     try
                //     {
                //         if(!process.HasExited && !OperatingSystem.IsIOS() &&!OperatingSystem.IsTvOS())
                //             process.Kill(killEntireProcessTreeWhenCancelled);
                //     }
                //     catch (Exception e)
                //     {
                //         tcs.TrySetException(e);
                //     }
                //     finally
                //     {
                //         tcs.TrySetCanceled(cancellationToken.Value);
                //     }
                // });
                // // ReSharper disable once AccessToDisposedClosure
                // process.Exited += (sender, args) => disposable.Dispose();
                // try
                // {
                //     if (process.HasExited) disposable.Dispose();
                // }
                // catch (Exception)
                // {
                //     //
                // }
                
                //
                
                cancelTask = Task.Run(async () =>
                {
                    while (!cancellationToken.Value.IsCancellationRequested)
                    {
                        await Task.Yield();
                        await 4.Milliseconds();
                    }
                    try
                    {
                        if(!process.HasExited && !OperatingSystem.IsIOS() &&!OperatingSystem.IsTvOS())
                            process.Kill(killEntireProcessTreeWhenCancelled);
                    }
                    catch (Exception e)
                    {
                        // tcs.TrySetException(e);
                    }
                    finally
                    {
                        if (throwTaskCancelledException)
                            tcs.TrySetCanceled(cancellationToken.Value);
                    }
                });
            }
            process.Exited += (sender, args) => tcs.TrySetResult(process.ExitCode);
            if (process.HasExited) tcs.TrySetResult(process.ExitCode);
            await Task.Yield();
            if (cancelTask is not null)
            {
                try
                {
                    await cancelTask.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    //
                }
            }
            return await tcs.Task.ConfigureAwait(false);
        });
}