using System.Collections.Concurrent;

namespace Binkus.DependencyInjection;

public record ServiceScopeId(Guid Id)
{
    public ServiceScopeId() : this(Guid.NewGuid()) { }
    public static readonly ServiceScopeId Empty = new(Guid.Empty); 
    public static implicit operator ServiceScopeId(Guid id) => new(id);
    public static implicit operator Guid(ServiceScopeId id) => id.Id;
}

public static class ServiceScopeManagerExt
{
    public static IServiceCollection AddServiceScopeManager(this IServiceCollection services)
    {
        services
            .AddScoped<ServiceScopeId>()
            .AddSingleton<ServiceScopeManager>();

        // MS EXT DI IServiceScopeFactory not able to decorate ServiceProviderEngineScope, not even with Scrutor,
        // cause the ServiceProviderEngineScope on ctor adds a IServiceScopeFactory that seems to be not overridable
        // and it would not be in IServiceCollection here, which would be necessary for Scrutor (cause Scrutor modifies
        // the implementation factory), within normal implementation factory MS EXT DI does not allow requesting
        // the same service that is about to register, so no decorating possible with that (would lead to StackOverflow).
        // services.Decorate<IServiceScopeFactory, ServiceScopeManager>();
        // services.AddSingleton<IServiceScopeFactory>(...);
        // services.AddScoped<IServiceScopeFactory, ServiceScopeManager>();
        // services.AddScoped<IServiceScopeFactory, ServiceScopeManager>(p => p.GetRequiredService<ServiceScopeManager>());

        return services;
    }

    public static ServiceScopeId GetServiceScopeId<TServiceScope>(this TServiceScope scope)
        where TServiceScope : IServiceScope
        => scope.ServiceProvider.GetRequiredService<ServiceScopeId>();
    
    public static ServiceScopeId GetServiceScopeId(this IServiceProvider provider)
        => provider.GetRequiredService<ServiceScopeId>();
    
    public static AsyncServiceScope AddToScopeManager<TServiceScope>(this TServiceScope scope)
        where TServiceScope : IServiceScope
        => scope.ServiceProvider.GetRequiredService<ServiceScopeManager>().AddScope(scope);
    
    public static AsyncServiceScope CreateScopeForScopeManager<TServiceScope>(this TServiceScope scope)
        where TServiceScope : IServiceScope
        => scope.ServiceProvider.GetRequiredService<ServiceScopeManager>().CreateScope();
    public static AsyncServiceScope CreateScopeForScopeManager(this IServiceProvider provider) 
        => provider.GetRequiredService<ServiceScopeManager>().CreateScope();
    
    public static AsyncServiceScope SetAsCurrentScope(this IServiceProvider provider)
        => provider.GetRequiredService<ServiceScopeManager>().SetCurrentScope(provider);
    
    public static AsyncServiceScope SetAsCurrentScope<TServiceScope>(this TServiceScope scope)
        where TServiceScope : IServiceScope
        => scope.ServiceProvider.GetRequiredService<ServiceScopeManager>().SetCurrentScope(scope.ServiceProvider);
}

public class ServiceScopeManager : IServiceScopeFactory
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

    IServiceScope IServiceScopeFactory.CreateScope() => CreateScope();
    public AsyncServiceScope CreateScope()
    {
        // var scope = new AsyncScopeWrapper(RootServices.CreateAsyncScope());
        var scope = ScopeFactory.CreateAsyncScope();
        var id = scope.GetServiceScopeId();
        if (_scopes.Count == 0)
        {
            // MainScopeId = CurrentScopeId = scope.Id;
            MainScopeId = CurrentScopeId = id;
        }
        _scopes.TryAdd(id, scope);
        return scope;
    }
    
    public AsyncServiceScope AddScope<TScope>(TScope serviceScope) where TScope : IServiceScope
    {
        var scope = serviceScope is AsyncServiceScope asyncS ? asyncS : new AsyncServiceScope(serviceScope);
        var id = scope.GetServiceScopeId();
        if (_scopes.Count == 0)
        {
            // MainScopeId = CurrentScopeId = scope.Id;
            MainScopeId = CurrentScopeId = id;
        }
        _scopes.TryAdd(id, scope);
        return scope;
    }
    
    public AsyncServiceScope ReplaceMainScope(bool disposeOldMainScope = false, AsyncServiceScope? newScope = null)
    {
        var mainScope = GetMainScope();
        _scopes.TryRemove(mainScope.GetServiceScopeId(), out _);
        if (disposeOldMainScope)
            mainScope.Dispose();
            // mainScope.DisposeAsync();
            // RxApp.TaskpoolScheduler.ScheduleAsync(mainScope, async (_, scope, _) => await scope.DisposeAsync());
        var newMainScope = newScope ?? CreateScope();
        if (newScope.HasValue)
        {
            var newScopeId = newScope.Value.GetServiceScopeId();
            if(_scopes.Keys.All(x => x != newScopeId))
                _scopes.TryAdd(newScopeId, newScope.Value);
        }
        MainScopeId = newMainScope.GetServiceScopeId();
        return newMainScope;
    }

    public AsyncServiceScope SetCurrentScope(IServiceProvider scopedServiceProvider)
    {
        if (ReferenceEquals(scopedServiceProvider, RootServices)) //return GetCurrentScope();
            throw new InvalidOperationException("Don't provide root ServiceProvider. Invalid scope.");
        return SetCurrentScope(scopedServiceProvider.GetServiceScopeId());
    }
    
    public AsyncServiceScope SetCurrentScope(ServiceScopeId serviceScopeId)
    {
        if (serviceScopeId == ServiceScopeId.Empty) // return GetCurrentScope();
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
        // return _scopes.FirstOrDefault(x => x.Key == mainScopeId).Value;
    }
    
    private volatile ServiceScopeId _currentScopeId;
    public ServiceScopeId CurrentScopeId { get => _currentScopeId; private set => _currentScopeId = value; }

    public AsyncServiceScope GetCurrentScope()
    {
        var currentScopeId = _currentScopeId;
        lock (currentScopeId) return _scopes[currentScopeId];
        // return _scopes.FirstOrDefault(x => x.Key == currentScopeId).Value;
    }
}

[Obsolete]
public readonly struct AsyncScopeWrapper : IServiceScope, IAsyncDisposable, IProvideServices
{
    private readonly AsyncServiceScope _scope;
    public AsyncScopeWrapper(in AsyncServiceScope scope)
    {
        _scope = scope;
        Id = Services.GetServiceScopeId();
    }
    // public AsyncScopeWrapper() : this(Globals.Services.CreateAsyncScope()) { }

    public ServiceScopeId Id { get; }

    IServiceProvider IServiceScope.ServiceProvider => _scope.ServiceProvider;
    public IServiceProvider Services => _scope.ServiceProvider;
    
    object? IServiceProvider.GetService(Type serviceType) => _scope.ServiceProvider.GetService(serviceType);
    public object? GetService(Type serviceType) => _scope.ServiceProvider.GetService(serviceType);
    public TService? GetService<TService>() => _scope.ServiceProvider.GetService<TService>();
    public TService GetRequiredService<TService>() where TService : notnull => _scope.ServiceProvider.GetRequiredService<TService>();
    public object GetRequiredService(Type serviceType) => _scope.ServiceProvider.GetRequiredService(serviceType);
    
    public void Dispose() => _scope.Dispose();
    public ValueTask DisposeAsync() => _scope.DisposeAsync();
}