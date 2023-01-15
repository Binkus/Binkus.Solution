#if NETSTANDARD2_0 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472 || NET48

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable CheckNamespace

namespace System.Collections.Generic;

public static class CollectionExtensions
{
    /// <summary>Tries to get the value associated with the specified <paramref name="key" /> in the <paramref name="dictionary" />.</summary>
    /// <param name="dictionary">A dictionary with keys of type <typeparamref name="TKey" /> and values of type <typeparamref name="TValue" />.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="dictionary" /> is <see langword="null" />.</exception>
    /// <returns>A <typeparamref name="TValue" /> instance. When the method is successful, the returned object is the value associated with the specified <paramref name="key" />. When the method fails, it returns the <see langword="default" /> value for <typeparamref name="TValue" />.</returns>
    public static TValue? GetValueOrDefault<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        TKey key)
    {
        return dictionary.GetValueOrDefault<TKey, TValue>(key, default (TValue)!);
    }
    
    /// <summary>Tries to get the value associated with the specified <paramref name="key" /> in the <paramref name="dictionary" />.</summary>
    /// <param name="dictionary">A dictionary with keys of type <typeparamref name="TKey" /> and values of type <typeparamref name="TValue" />.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="defaultValue">The default value to return when the <paramref name="dictionary" /> cannot find a value associated with the specified <paramref name="key" />.</param>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="dictionary" /> is <see langword="null" />.</exception>
    /// <returns>A <typeparamref name="TValue" /> instance. When the method is successful, the returned object is the value associated with the specified <paramref name="key" />. When the method fails, it returns <paramref name="defaultValue" />.</returns>
    public static TValue GetValueOrDefault<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue defaultValue)
    {
        TValue obj;
        return !dictionary.TryGetValue(key, out obj) ? defaultValue : obj;
    }
}
#endif