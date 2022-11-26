namespace DDS.Core.Helper;

public static class TaskExtensions
{
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
}

// internal static class TaskExtTesttatai
// {
    // public static async Task<Some<T>> Try<T>(this Task<T> task)
    // {
    //     try
    //     {
    //         await Task.Yield();
    //         var value = await task;
    //         return new Some<T>{ Value = value };
    //     }
    //     catch (Exception e)
    //     {
    //         return new Some<T>{ Ex = e };
    //     }
    // }
    //
    // public readonly struct Some<T>
    // {
    //     [UsedImplicitly] public T Value { get; init; }
    //     [UsedImplicitly] public Exception Ex { get; init; }
    // }
// }