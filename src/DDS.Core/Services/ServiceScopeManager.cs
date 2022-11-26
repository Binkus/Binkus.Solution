namespace DDS.Core.Services;

public class ServiceScopeManager
{
    public IServiceProvider RootServices { get; }

    public ServiceScopeManager() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor]
    public ServiceScopeManager(IServiceProvider serviceProvider)
    {
        RootServices = serviceProvider;
        Scopes = _scopes = new List<AsyncScopeWrapper>();
    }

    private readonly List<AsyncScopeWrapper> _scopes;
    public IReadOnlyList<AsyncScopeWrapper> Scopes { get; }

    public AsyncScopeWrapper CreateScope()
    {
        var scope = new AsyncScopeWrapper(RootServices.CreateAsyncScope());
        if (_scopes.Count == 0)
        {
            MainScopeId = scope.Id;
        }
        _scopes.Add(scope);
        return scope;
    }
    
    public AsyncScopeWrapper ReplaceMainScope(bool disposeOldMainScope = false, AsyncScopeWrapper? newScope = null)
    {
        var mainScope = GetMainScope();
        _scopes.Remove(mainScope);
        if (disposeOldMainScope)
            mainScope.DisposeAsync();
        var newMainScope = newScope ?? CreateScope();
        if (newScope.HasValue)
        {
            if(_scopes.All(x => x.Id != newScope.Value.Id))
                _scopes.Add(newScope.Value);
        }
        MainScopeId = newMainScope.Id;
        return newMainScope;
    }

    public AsyncScopeWrapper GetScope(Guid id)
    {
        return _scopes.First(x => x.Id == id);
    }

    public Guid MainScopeId { get; private set; }

    public AsyncScopeWrapper GetMainScope()
    {
        return _scopes.FirstOrDefault(x => x.Id == MainScopeId);
    }
}

public readonly struct AsyncScopeWrapper : IServiceScope, IAsyncDisposable, IProvideServices
{
    private readonly AsyncServiceScope _scope;

    
    public AsyncScopeWrapper() => _scope = Globals.Services.CreateAsyncScope();

    public AsyncScopeWrapper(in AsyncServiceScope scope) => _scope = scope;


    // public uint OrderId { get; } = Interlocked.

    public Guid Id { get; } = Guid.NewGuid();

    public IServiceProvider ServiceProvider => _scope.ServiceProvider;
    public IServiceProvider Services => _scope.ServiceProvider;
    
    public TService GetService<TService>() where TService : notnull => Services.GetRequiredService<TService>();
    public object GetService(Type serviceType) => Services.GetRequiredService(serviceType);
    
    public void Dispose() => _scope.Dispose();
    public ValueTask DisposeAsync() => _scope.DisposeAsync();
}