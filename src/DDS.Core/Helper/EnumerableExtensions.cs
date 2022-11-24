using System.Runtime.CompilerServices;

namespace DDS.Core.Helper;

public static class EnumerableExtensions
{
    public static TaskAwaiter GetAwaiter(this IEnumerable<Task> tasks) => TryWhenAllAsync(tasks).GetAwaiter();
    
    public static TaskAwaiter<T[]> GetAwaiter<T>(this IEnumerable<Task<T>> tasks) => TryWhenAllAsync(tasks).GetAwaiter();
    
    public static TaskAwaiter<List<T>> GetAwaiter<T>(this IEnumerable<ValueTask<T>> tasks) => AwaitEnumerable(tasks).GetAwaiter();
    
    public static TaskAwaiter GetAwaiter(this IEnumerable<ValueTask> tasks) => AwaitEnumerable(tasks).GetAwaiter();

    [UsedImplicitly]
    public static async Task<T[]> TryWhenAllAsync<T>(this IEnumerable<Task<T>> tasks)
    {
        var result = Task.WhenAll(tasks);

        try
        {
            return await result.ConfigureAwait(false);
        }
        catch (Exception) { /* ignore */ }

        throw result.Exception 
              ?? throw new UnreachableException("AggregateException was null, which is not possible.");
    }

    [UsedImplicitly]
    public static async Task TryWhenAllAsync(this IEnumerable<Task> tasks)
    {
        var result = Task.WhenAll(tasks);

        try
        {
            await result.ConfigureAwait(false);
            return;
        }
        catch (Exception) { /* ignore */ }

        throw result.Exception 
              ?? throw new UnreachableException("AggregateException was null, which is not possible.");
    }

    private static async Task AwaitEnumerable(IEnumerable<ValueTask> tasks)
    {
        // var valueTasks = tasks as ValueTask[] ?? tasks.ToArray();
        // foreach (var valueTask in valueTasks)
        // {
        //     var _ = valueTask.ConfigureAwait(false);
        // }

#pragma warning disable CS8073
        tasks = tasks.Select(t => t != null ? t : throw new ArgumentException()).ToList();
#pragma warning restore CS8073
        
        foreach (var valueTask in tasks)
        {
            await valueTask.ConfigureAwait(false);
        }
    }
    
    private static async Task<List<T>> AwaitEnumerable<T>(IEnumerable<ValueTask<T>> tasks)
    {
        var results = new List<T>();
        
#pragma warning disable CS8073
        tasks = tasks.Select(t => t != null ? t : throw new ArgumentException()).ToList();
#pragma warning restore CS8073
        
        foreach (var valueTask in tasks)
        {
            results.Add(await valueTask.ConfigureAwait(false));
        }

        return results;
    }
}