using System.Runtime.CompilerServices;

namespace DDS.Core.Helper;

public static class LazyExtensions
{
    public static ValueTaskAwaiter<T> GetAwaiter<T>(this Lazy<T> lazy) => ValueTask.FromResult(lazy.Value).GetAwaiter();
}