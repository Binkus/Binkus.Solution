using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Binkus.DependencyInjection;

// public sealed record ServiceScopeId(Guid Id)
// {
//     public ServiceScopeId() : this(Guid.NewGuid()) { }
//     
//     public static readonly ServiceScopeId Empty = new(Guid.Empty); 
//     public static implicit operator ServiceScopeId(Guid id) => new(id);
//     public static implicit operator Guid(ServiceScopeId id) => id.Id;
// }

public static class ServiceScopeManagerExt
{
    public static IServiceCollection AddServiceScopeManager(this IServiceCollection services, 
        Func<IServiceProvider, Action<IServiceProvider, Action<ServiceScopeId>>, IServiceScopeManager>? serviceScopeManagerFactory = null)
    {
        services
            .AddScoped<ServiceScopeId>()
            .AddScoped<IScopeDisposer, CancellationDisposableWrapper>();

        return serviceScopeManagerFactory is null
            ? services.AddSingleton<IServiceScopeManager, ServiceScopeManager>()
            : services.AddSingleton<IServiceScopeManager>(p => serviceScopeManagerFactory(p,
                static (scopedProvider, onServiceScopeDisposal) => scopedProvider.GetRequiredService<IScopeDisposer>()
                    .Token.Register(() 
                        => onServiceScopeDisposal(scopedProvider.GetServiceScopeId()), true)));
    }

    public static ServiceScopeId GetServiceScopeId<TServiceScope>(this TServiceScope scope)
        where TServiceScope : IServiceScope
        => scope.ServiceProvider.GetRequiredService<ServiceScopeId>();
    
    public static ServiceScopeId GetServiceScopeId(this IServiceProvider provider)
        => provider.GetRequiredService<ServiceScopeId>();
    
    public static AsyncServiceScope AddToScopeManager<TServiceScope>(this TServiceScope scope)
        where TServiceScope : IServiceScope
        => scope.ServiceProvider.GetRequiredService<IServiceScopeManager>().AddScope(scope);
    
    public static AsyncServiceScope CreateScopeForScopeManager<TServiceScope>(this TServiceScope scope)
        where TServiceScope : IServiceScope
        => scope.ServiceProvider.GetRequiredService<IServiceScopeManager>().CreateScope();
    public static AsyncServiceScope CreateScopeForScopeManager(this IServiceProvider provider) 
        => provider.GetRequiredService<IServiceScopeManager>().CreateScope();
    
    public static AsyncServiceScope SetAsCurrentScope(this IServiceProvider provider)
        => provider.GetRequiredService<IServiceScopeManager>().SetCurrentScope(provider);
    
    public static AsyncServiceScope SetAsCurrentScope<TServiceScope>(this TServiceScope scope)
        where TServiceScope : IServiceScope
        => scope.ServiceProvider.GetRequiredService<IServiceScopeManager>().SetCurrentScope(scope.ServiceProvider);
}

public interface IServiceScopeManager : IServiceScopeFactory
{
    IServiceProvider RootServices { get; }
    IServiceScopeFactory ScopeFactory { get; }
    IReadOnlyDictionary<ServiceScopeId, AsyncServiceScope> Scopes { get; }
    ServiceScopeId MainScopeId { get; }
    ServiceScopeId CurrentScopeId { get; }
    IServiceScope IServiceScopeFactory.CreateScope() => CreateScope();
    new AsyncServiceScope CreateScope();
    AsyncServiceScope? RemoveScope(ServiceScopeId serviceScopeId, bool dispose = true);
    AsyncServiceScope AddScope<TScope>(TScope serviceScope) where TScope : IServiceScope;
    AsyncServiceScope ReplaceMainScope(bool disposeOldMainScope = false, AsyncServiceScope? newScope = null);
    AsyncServiceScope SetCurrentScope(IServiceProvider scopedServiceProvider);
    AsyncServiceScope SetCurrentScope(ServiceScopeId serviceScopeId);
    AsyncServiceScope GetScope(IServiceProvider provider);
    AsyncServiceScope GetScope(ServiceScopeId id);
    AsyncServiceScope GetMainScope();
    AsyncServiceScope GetCurrentScope();
}

file sealed class ServiceScopeManager : IServiceScopeManager
{
    public IServiceProvider RootServices { get; }
    public IServiceScopeFactory ScopeFactory { get; }

    [ActivatorUtilitiesConstructor]
    public ServiceScopeManager(IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory)
    {
        RootServices = serviceProvider;
        ScopeFactory = serviceScopeFactory;
        Scopes = _scopes = new ConcurrentDictionary<ServiceScopeId,AsyncServiceScope>();
        _mainScopeId = _currentScopeId = ServiceScopeId.Empty;
    }

    private readonly ConcurrentDictionary<ServiceScopeId, AsyncServiceScope> _scopes;
    public IReadOnlyDictionary<ServiceScopeId,AsyncServiceScope> Scopes { get; }

    // private void TryAdd(AsyncServiceScope scope) => TryAdd(scope.GetServiceScopeId(), scope);
    private void TryAdd(ServiceScopeId id, AsyncServiceScope scope)
    {
        scope.ServiceProvider.GetRequiredService<IScopeDisposer>().Token
            .Register(() => RemoveScope(id, false), true);
        _scopes.TryAdd(id, scope);
    }

    public AsyncServiceScope? RemoveScope(ServiceScopeId serviceScopeId, bool dispose = true)
    {
        if (dispose) GetScope(serviceScopeId).Dispose();
        return _scopes.TryRemove(serviceScopeId, out var removedItem) ? removedItem : null;
    }
    
    public AsyncServiceScope CreateScope() => AddScope(ScopeFactory.CreateAsyncScope());
    public AsyncServiceScope AddScope<TScope>(TScope serviceScope) where TScope : IServiceScope
    {
        var scope = serviceScope is AsyncServiceScope asyncS ? asyncS : new AsyncServiceScope(serviceScope);
        var id = scope.GetServiceScopeId();
        if (_scopes.Count == 0)
        {
            MainScopeId = CurrentScopeId = id;
        }
        TryAdd(id, scope);
        return scope;
    }
    
    public AsyncServiceScope ReplaceMainScope(bool disposeOldMainScope = false, AsyncServiceScope? newScope = null)
    {
        var mainScope = GetMainScope();
        _scopes.TryRemove(mainScope.GetServiceScopeId(), out _);
        if (disposeOldMainScope)
            mainScope.Dispose();
        var newMainScope = newScope ?? CreateScope();
        if (newScope.HasValue)
        {
            var newScopeId = newScope.Value.GetServiceScopeId();
            if(_scopes.Keys.All(x => x != newScopeId))
                TryAdd(newScopeId, newScope.Value);
        }
        MainScopeId = newMainScope.GetServiceScopeId();
        return newMainScope;
    }

    public AsyncServiceScope SetCurrentScope(IServiceProvider scopedServiceProvider)
    {
        if (ReferenceEquals(scopedServiceProvider, RootServices))
            throw new InvalidOperationException("Don't provide root ServiceProvider. Invalid scope.");
        return SetCurrentScope(scopedServiceProvider.GetServiceScopeId());
    }
    
    public AsyncServiceScope SetCurrentScope(ServiceScopeId serviceScopeId)
    {
        if (serviceScopeId == ServiceScopeId.Empty)
            throw new InvalidOperationException("Invalid scope.");
        CurrentScopeId = serviceScopeId;
        return GetCurrentScope();
    }
    
    public AsyncServiceScope GetScope(IServiceProvider provider)
    {
        var scopeId = provider.GetServiceScopeId();
        return _scopes.First(x => x.Key == scopeId).Value;
    }

    public AsyncServiceScope GetScope(ServiceScopeId id)
    {
        return _scopes.First(x => x.Key == id).Value;
    }

    private volatile ServiceScopeId _mainScopeId;
    public ServiceScopeId MainScopeId { get => _mainScopeId; private set => _mainScopeId = value; }

    public AsyncServiceScope GetMainScope()
    {
        var mainScopeId = _mainScopeId;
        lock (mainScopeId) return _scopes[mainScopeId];
    }
    
    private volatile ServiceScopeId _currentScopeId;
    public ServiceScopeId CurrentScopeId { get => _currentScopeId; private set => _currentScopeId = value; }

    public AsyncServiceScope GetCurrentScope()
    {
        var currentScopeId = _currentScopeId;
        lock (currentScopeId) return _scopes[currentScopeId];
    }
}

file interface IScopeDisposer : IAsyncDisposable, IDisposable { CancellationToken Token { get; } }
file class CancellationDisposableWrapper : IScopeDisposer
{
    public CancellationToken Token => _tokenSource.Token;
    private readonly CancellationTokenSource _tokenSource = new();

    public void Dispose() => _tokenSource.Cancel();

    public ValueTask DisposeAsync()
    {
        _tokenSource.Cancel();
        return default;
    }
}