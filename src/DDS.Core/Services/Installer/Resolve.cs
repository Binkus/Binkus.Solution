using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DDS.Core;
using DDS.Core.Helper;
using DDS.Core.Services;
using DDS.Core.ViewModels;

namespace DDS.Core.Services.Installer;

public static class ResolveInstaller
{
    private static IServiceCollection AddResolveResolution(this IServiceCollection services) 
        => services.AddTransient(
            typeof(Resolve<>),
            typeof(Resolve<>));
    
    private static IServiceCollection AddResolveLazilyResolution(this IServiceCollection services) 
        => services.AddTransient(
            typeof(ResolveLazily<>),
            typeof(ResolveLazily<>));
    
    private static IServiceCollection AddResolveLazilyResolution2(this IServiceCollection services) 
        => services.AddTransient(
            typeof(ResolveLazily<>),
            typeof(ResolveLazily<>));
}

public interface IResolve<out T>
{
    [UsedImplicitly] public T Value { get; }
    [UsedImplicitly] public bool IsValueCreated { get; }
    [UsedImplicitly] public IServiceProvider Services { get; }
}

    // public static TaskAwaiter<T> GetAwaiter<T>(this IResolve<T> @this)
    // {
    //     
    //     // if (typeof(T) is IAwaitable<ValueTaskAwaiter<T>> awaitable)
    //     // {
    //     //     return awaitable.GetAwaiter();
    //     // }
    //     // return Task.FromResult(@this.Value).GetAwaiter();
    // }

    public record Resolve<T> : IResolve<T>, IAwaitable<ValueTaskAwaiter<T>>
    {
        [UsedImplicitly, DebuggerBrowsable(DebuggerBrowsableState.Never)] public T Value { get; }
        [UsedImplicitly] public bool IsValueCreated => true;
        [UsedImplicitly] public IServiceProvider Services { get; }
        
        [ActivatorUtilitiesConstructor]
        public Resolve(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            Value = ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);
        }
        
        public ValueTaskAwaiter<T> GetAwaiter() => ValueTask.FromResult(Value).GetAwaiter();
    }

    public record class ResolveLazily<T> : IResolve<T>, IAwaitable<ValueTaskAwaiter<T>>
    {
        [UsedImplicitly, DebuggerBrowsable(DebuggerBrowsableState.Never)] public T Value => Lazy.Value;
        [UsedImplicitly] public bool IsValueCreated => Lazy.IsValueCreated;
        [UsedImplicitly] public Lazy<T> Lazy { get; }
        [UsedImplicitly] public IServiceProvider Services { get; }
        
        [ActivatorUtilitiesConstructor]
        public ResolveLazily(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            Lazy = new Lazy<T>(() => ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider));
        }

        public ValueTaskAwaiter<T> GetAwaiter() => ValueTask.FromResult(Value).GetAwaiter();
    }
    
    public record ResolveAsync<T> : IResolve<T>, IAwaitable<TaskAwaiter<T>>
    {
        [UsedImplicitly, DebuggerBrowsable(DebuggerBrowsableState.Never)] public T Value => Lazy.Value;
        [UsedImplicitly] public bool IsValueCreated => Lazy.IsValueCreated;
        [UsedImplicitly] public Task<T> ResolveTask { get; }
        [UsedImplicitly] public Lazy<T> Lazy { get; }
        [UsedImplicitly] public IServiceProvider Services { get; }
        
        [ActivatorUtilitiesConstructor]
        public ResolveAsync(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
            var l = Lazy = new Lazy<T>(() => ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider));
            ResolveTask = Task.Run(() => l.Value);
        }

        public TaskAwaiter<T> GetAwaiter() => ResolveTask.GetAwaiter();
    }