using Microsoft.Extensions.DependencyInjection;

namespace Binkus.DependencyInjection;

public static class IocAdapterRegistration
{
    public static IList<IocDescriptor> AddServiceScopeFactory(this IList<IocDescriptor> descriptors)
    {
        // IocDescriptor.CreateScoped<IContainerScopeFactory>(p => (IContainerScopeFactory)p);
        // IocDescriptor.CreateScoped<IServiceScopeFactory>(p => new ServiceScopeFactoryAdapter(p.GetRequiredService<IServiceScopeFactory>()));

        var d = IocDescriptor.CreateScoped<IServiceScopeFactory>(
            p => new ContainerScopeFactoryAdapter((IContainerScopeFactory)p));
        
        descriptors.Add(d);
        
        return descriptors;
    }
}

public interface IContainerScopeFactoryAdapter : IContainerScopeFactory, IServiceScopeFactory
{
    IContainerScope IContainerScopeFactory.CreateScope() => CreateScope();
    IServiceScope IServiceScopeFactory.CreateScope() => CreateScope();
    new IContainerScopeAdapter CreateScope();
}

public interface IContainerScopeAdapter : IContainerScope, IServiceScope, IContainerScopeFactoryAdapter, 
    IServiceScopeFactory, IAsyncDisposable, IDisposable
{
    IServiceProvider IServiceScope.ServiceProvider => Services;
}

public sealed record ContainerScopeFactoryAdapter : IContainerScopeFactoryAdapter
{
    private readonly IContainerScopeFactory _scopeFactoryImpl;
    public ContainerScopeFactoryAdapter(IContainerScopeFactory scopeFactoryImpl)
        => _scopeFactoryImpl = scopeFactoryImpl;

    public IContainerScopeAdapter CreateScope()
        => new ContainerScopeAdapter(_scopeFactoryImpl.CreateScope());
}

public sealed record ContainerScopeAdapter : IContainerScopeAdapter
{
    private readonly IContainerScope _scopeImpl;
    public ContainerScopeAdapter(IContainerScope scopeImpl)
        => _scopeImpl = scopeImpl;

    public IContainerScopeAdapter CreateScope()
        => new ContainerScopeAdapter(_scopeImpl.CreateScope());

    public IServiceProvider Services => _scopeImpl.Services;
    public void Dispose() => _scopeImpl.Dispose();
    public ValueTask DisposeAsync() => _scopeImpl.DisposeAsync();
}

//

public sealed record ServiceScopeFactoryAdapter : IContainerScopeFactoryAdapter
{
    private readonly IServiceScopeFactory _scopeFactoryImpl;
    public ServiceScopeFactoryAdapter(IServiceScopeFactory scopeFactoryAdapterImpl)
        => _scopeFactoryImpl = scopeFactoryAdapterImpl;

    public IContainerScopeAdapter CreateScope()
        => new ServiceScopeAdapter(_scopeFactoryImpl.CreateScope());
}

public sealed record ServiceScopeAdapter : IContainerScopeAdapter
{
    private readonly IServiceScope _scopeImpl;
    public ServiceScopeAdapter(IServiceScope scopeAdapterImpl)
        => _scopeImpl = scopeAdapterImpl;

    public IContainerScopeAdapter CreateScope()
        => new ServiceScopeAdapter(_scopeImpl.ServiceProvider.CreateAsyncScope());

    public IServiceProvider Services => _scopeImpl.ServiceProvider;
    
    public void Dispose() => _scopeImpl.Dispose();
    public ValueTask DisposeAsync()
    {
        if (_scopeImpl is IAsyncDisposable a)
            return a.DisposeAsync();
        _scopeImpl.Dispose();
        return default;
    }
}