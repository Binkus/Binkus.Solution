#if !NET6_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace Binkus.Compat;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
// [TypeForwardedTo(typeof(global::System.ArgumentNullException))]
public static class ArgumentNullException
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null) throw new global::System.ArgumentNullException(paramName);
    }
}
#endif