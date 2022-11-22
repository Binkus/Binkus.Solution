namespace DDS.Core.Helper;

public static class TaskExtensions
{
    public static async ValueTask YieldAndContinue(this ValueTask valueTask)
    {
        await Task.Yield();
        await valueTask;
    }
    
    public static async ValueTask<T> YieldAndContinue<T>(this ValueTask<T> valueTask)
    {
        await Task.Yield();
        return await valueTask;
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
}