#if !NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace System;

internal static class TypeExt
{
    internal static bool IsAssignableTo(
        [NotNullWhen(true)] this Type? fromType,
        [NotNullWhen(true)] Type? targetType) =>
        targetType?.IsAssignableFrom(fromType) ?? false;
}
#endif