using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DDS.Core.Helper;

public static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOrDefault<T>(this WeakReference<T> weakReference) where T : class?
        => weakReference.TryGetTarget(out var target) ? target : default;

    public static void ThrowWhenNotNull(this Exception? exception)
    {
        if (exception is not null) throw exception;
    }
    
    public static TItem AddTo<TItem, TCollection>(this TItem item, TCollection collection)
        where TCollection : ICollection<TItem>
    {
        collection.Add(item);
        return item;
    }
    
    public static TItem? IfNotNullAddTo<TItem, TCollection>(this TItem? item, TCollection collection, Action<TItem?>? actionBeforeAdd = null)
        where TCollection : ICollection<TItem>
    {
        actionBeforeAdd?.Invoke(item);
        if (item is not null) collection.Add(item);
        return item;
    }
    
    // public static TItem IfNotNullAddTo<TItem, TCollection>(this TItem? item, TCollection collection, Func<TItem?,TItem> funcBeforeAdd)
    //     where TCollection : ICollection<TItem>
    // {
    //     var itemToAdd = funcBeforeAdd.Invoke(item);
    //     if (itemToAdd is not null) collection.Add(itemToAdd);
    //     return itemToAdd;
    // }
    
    public static TItemToAdd IfNotNullAddTo<TItem, TItemToAdd, TCollection>(this TItem? item, TCollection collection, Func<TItem?,TItemToAdd> funcBeforeAdd)
        where TCollection : ICollection<TItemToAdd>
        where TItemToAdd : notnull
    {
        var itemToAdd = funcBeforeAdd.Invoke(item);
        collection.Add(itemToAdd);
        return itemToAdd;
    }

    // public static TCollection AddToReturnCollection<TItem, TCollection>(this TItem item, TCollection collection)
    //     where TCollection : ICollection<TItem>
    // {
    //     collection.Add(item);
    //     return collection;
    // }
    
    //

    public static void InvokeAndThrowWith(this Action action, IScheduler? scheduler = default)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            scheduler ??= RxApp.MainThreadScheduler;
            scheduler.Schedule(e, (_, exception) => throw exception);
        }
    }
    
    public static TResult? InvokeAndThrowWith<TResult>(this Func<TResult> func, IScheduler? scheduler = default)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            scheduler ??= RxApp.MainThreadScheduler;
            scheduler.Schedule(e, (_, exception) => throw exception);
        }

        return default;
    }
    
    public static async Task InvokeAndThrowWith<TResult>(this Func<Task> func, IScheduler? scheduler = default)
    {
        try
        {
            await func();
        }
        catch (Exception e)
        {
            scheduler ??= RxApp.MainThreadScheduler;
            scheduler.Schedule(e, (_, exception) => throw exception);
        }
    }
    
    public static async Task InvokeAndThrowWith<TResult>(this Func<CancellationToken, Task> func, CancellationToken token, IScheduler? scheduler = default)
    {
        try
        {
            await func(token);
        }
        catch (Exception e)
        {
            scheduler ??= RxApp.MainThreadScheduler;
            scheduler.Schedule(e, (_, exception) => throw exception);
        }
    }
    
    public static async Task<TResult?> InvokeAndThrowWith<TResult>(this Func<Task<TResult>> func, IScheduler? scheduler = default)
    {
        try
        {
            return await func();
        }
        catch (Exception e)
        {
            scheduler ??= RxApp.MainThreadScheduler;
            scheduler.Schedule(e, (_, exception) => throw exception);
        }

        return default;
    }
    
    public static async Task<TResult?> InvokeAndThrowWith<TResult>(this Func<CancellationToken, Task<TResult>> func, CancellationToken token, IScheduler? scheduler = default)
    {
        try
        {
            return await func(token);
        }
        catch (Exception e)
        {
            scheduler ??= RxApp.MainThreadScheduler;
            scheduler.Schedule(e, (_, exception) => throw exception);
        }

        return default;
    }
    
    //
    
    public static (TLeft, TRight) CreateTuple<TLeft, TRight>(this TLeft leftPrepend, TRight rightAppend)
        => (leftPrepend, rightAppend);
    public static (TLeft, TRight) CreateTupleAppend<TRight, TLeft>(
        this TRight rightAppend, TLeft leftPrependToFront) 
        => (leftPrependToFront, rightAppend);
    public static (TLeft, TMiddle, TRight) CreateTuple<TLeft, TMiddle, TRight>(
        this TLeft left, TMiddle middle, TRight right)
        => (left, middle, right);
    
    public static (TLeft, TMiddle, TRight) CreateTupleInsertInMiddle<TLeft, TMiddle, TRight>(
        this TMiddle middle, TLeft left, TRight right)
        => (left, middle, right);
    
    //
    
    public static T Inject<T>(this T anything, Action action)
    {
        action.Invoke();
        return anything;
    }
    
    public static T Inject<T>(this T anything, Action<T> action)
    {
        action.Invoke(anything);
        return anything;
    }
    
    public static T Inject<T, TIgnore>(this T anything, Func<TIgnore> func)
    {
        func.Invoke();
        return anything;
    }
    
    public static T Inject<T, TIgnore>(this T anything, Func<T,TIgnore> func)
    {
        func.Invoke(anything);
        return anything;
    }
    
    public static async Task<T> InjectAsync<T>(this Task<T> anything, Func<Task> func)
    {
        await func.Invoke();
        return await anything;
    }
    
    public static async Task<T> InjectAsync<T>(this T anything, Func<Task> func)
    {
        await func.Invoke().ConfigureAwait(false);
        return anything;
    }
    
    public static async Task<T> InjectAsync<T>(this T anything, Func<T,Task> func)
    {
        await func.Invoke(anything).ConfigureAwait(false);
        return anything;
    }
}



