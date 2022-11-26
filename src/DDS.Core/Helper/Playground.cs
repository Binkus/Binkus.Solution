namespace DDS.Core.Helper;

public static class Playground
{
    // public static async Task<IEnumerable<T>> WhenAll<T>(this IEnumerable<Task<T>> tasks)
    // {
    //     var allTasks = Task.WhenAll(tasks);
    //
    //     try
    //     {
    //         return await allTasks;
    //     }
    //     catch (Exception)
    //     {
    //         //
    //     }
    //
    //     throw allTasks.Exception 
    //           ?? throw new UnreachableException("AggregateException of all tasks was null. What the hell.");
    // }
    //
    // public static Task<IEnumerable<T>> WhenAll<T>(this Task<T>[] tasks) => ((IEnumerable<Task<T>>)tasks).WhenAll();

    private enum E { A, B }
    private static E GetE() => E.A;
    private static string UnreachableExceptionExample()
    {
        var e = GetE();
        return e switch
        {
            E.A => "A",
            E.B => "B",
            _ => throw new UnreachableException($"Tried to parse enum E of int {(int)e}")
        };
    }
}