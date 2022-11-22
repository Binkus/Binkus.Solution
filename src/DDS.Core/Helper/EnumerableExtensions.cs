using System.Runtime.CompilerServices;

namespace DDS.Core.Helper;

public static class EnumerableExtensions
{
    public static TaskAwaiter GetAwaiter(this IEnumerable<Task> tasks) => WhenAllAsync(tasks).GetAwaiter();
    
    public static TaskAwaiter<T[]> GetAwaiter<T>(this IEnumerable<Task<T>> tasks) => WhenAllAsync(tasks).GetAwaiter();
    
    public static TaskAwaiter<List<T>> GetAwaiter<T>(this IEnumerable<ValueTask<T>> tasks) => AwaitEnumerable(tasks).GetAwaiter();
    
    public static TaskAwaiter GetAwaiter(this IEnumerable<ValueTask> tasks) => AwaitEnumerable(tasks).GetAwaiter();

    public static async Task<T[]> WhenAllAsync<T>(this IEnumerable<Task<T>> tasks)
    {
        var result = Task.WhenAll(tasks);

        try
        {
            return await result;
        }
        catch (Exception)
        {
            //
        }

        throw result.Exception 
              ?? throw new UnreachableException("AggregateException was null, which is not possible.");
    }

    public static async Task WhenAllAsync(this IEnumerable<Task> tasks)
    {
        var result = Task.WhenAll(tasks);

        try
        {
            await result;
            return;
        }
        catch (Exception)
        {
            //
        }

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
        
        foreach (var valueTask in tasks)
        {
            await valueTask.ConfigureAwait(false);
        }
    }
    
    private static async Task<List<T>> AwaitEnumerable<T>(IEnumerable<ValueTask<T>> tasks)
    {
        var results = new List<T>();
        
        foreach (var valueTask in tasks)
        {
            results.Add(await valueTask.ConfigureAwait(false));
        }

        return results;
    }
}