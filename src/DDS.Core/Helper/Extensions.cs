using System.Runtime.CompilerServices;

namespace DDS.Core.Helper;

public static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOrDefault<T>(this WeakReference<T> weakReference) where T : class?
        => weakReference.TryGetTarget(out var target) ? target : default;
}