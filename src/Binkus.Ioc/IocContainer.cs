using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Binkus.DependencyInjection;

public sealed class IocContainer //: IServiceProvider
{
    private static IEnumerable<KeyValuePair<Type, IocDescriptor>> ToKeyValuePair(IEnumerable<IocDescriptor> services) 
        => services.Select(d => new KeyValuePair<Type, IocDescriptor>(d.ServiceType, d));
    
    public IocContainer(IEnumerable<IocDescriptor> services, bool readOnly = true) : this(services, null, readOnly) { }
    public IocContainer(IEnumerable<IocDescriptor> services, ServiceScopeId? id, bool readOnly = true) : this(readOnly)
    {
        RootContainerScope = new IocContainerScope(id);
    }

    public IocContainer(ServiceScopeId? id = null) : this(false)
    {
        RootContainerScope = new IocContainerScope(id);
    }

#nullable disable
    private IocContainer(bool readOnly)
    {
        IsReadOnly = readOnly;
    }
#nullable enable

    public bool IsReadOnly { get; internal set; }
    
    public IocContainerScope RootContainerScope { get; set; }
    public IocContainerScope ContainerScope { get; set; }
}



// Scope Engine

public sealed record IocContainerScope : IDisposable, IAsyncDisposable, IServiceProvider
{
    public IocContainerScope RootContainerScope { get; init; }
    public IocContainerScope ParentContainerScope { get; init; }
    public ServiceScopeId Id { get; init; }
    public bool IsRootContainerScope { get; init; }

    internal IocContainerScope(ServiceScopeId? id = null)
    {
        RootContainerScope = this;
        ParentContainerScope = this;
        Id = id ?? new ServiceScopeId();
        IsRootContainerScope = true;
    }
    
    internal IocContainerScope(IocContainerScope rootContainerScope, IocContainerScope parentContainerScope, ServiceScopeId? id = null)
    {
        RootContainerScope = rootContainerScope;
        ParentContainerScope = parentContainerScope;
        Id = id ?? new ServiceScopeId();
        IsRootContainerScope = false;
    }

    public object? GetService(Type serviceType)
    {
        return null;
    }
    
    private object? GetServiceForDescriptor2(IocDescriptor descriptor)
        => descriptor.Lifetime switch
        {
            IocLifetime.Singleton => GetSingletonService(descriptor),
            IocLifetime.Scoped => GetScopedService(descriptor),
            IocLifetime.Transient => GetTransientService(descriptor),
            _ => null
        };

    internal object? GetSingletonService(IocDescriptor descriptor)
    {
        // if (descriptor.Implementation is { }) return descriptor.Implementation;
        // if (descriptor.Factory?.Invoke(ContainerProvider) is { } serviceImpl) return serviceImpl;
        // lock (descriptor)
        // {
        //     return descriptor.Implementation ??= TryCreateService(ContainerProvider, descriptor);    
        // }

        return null;
    }

    internal object? GetScopedService(IocDescriptor descriptor) => GetSingletonService(descriptor); // it is scoped already
    
    internal object? GetTransientService(IocDescriptor descriptor)
    {
        // if (descriptor.Factory?.Invoke(ContainerProvider) is { } serviceImpl) return serviceImpl;
        // return TryCreateService(ContainerProvider, descriptor);

        return null;
    }

    internal static object? TryCreateService(IServiceProvider provider, IocDescriptor descriptor)
    {
        var type = descriptor.ImplType ?? descriptor.ServiceType;
        if (type.IsAbstract) return null;

        // return IocUtilities.CreateInstance(provider, type);

        return Activator.CreateInstance(type);
    }
    
    // Disposal:

    private async ValueTask AsyncDispose()
    {
        // async dispose:
        IEnumerable<IocDescriptor> descriptors = null!;//ContainerProvider.Descriptors.Values; // todo
        foreach (var descriptor in descriptors)
        {
            if (!IsRootContainerScope && descriptor.Lifetime is not IocLifetime.Scoped) continue;
            // only scoped services get disposed as well as singletons if this represents the root IoC Container:
            var impl = descriptor.Implementation ?? null; // todo get from singleton collection
            switch (impl)
            {
                case null:
                    continue;
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                default:
                    (descriptor.Implementation as IDisposable)?.Dispose();
                    break;
            }
        }
    }
    private void SyncDispose()
    {
        // sync dispose:
        IEnumerable<IocDescriptor> descriptors = null!;//ContainerProvider.Descriptors.Values; // todo
        foreach (var descriptor in descriptors)
        {
            if (!IsRootContainerScope && descriptor.Lifetime is not IocLifetime.Scoped) continue;
            // only scoped services get disposed as well as singletons if this represents the root IoC Container:
            var impl = descriptor.Implementation ?? null; // todo get from singleton collection
            switch (impl)
            {
                case null:
                    continue;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                default:
                    (descriptor.Implementation as IAsyncDisposable)?.DisposeAsync().GetAwaiter().GetResult();
                    break;
            }
        }
    }
    
    private readonly object _mutex = new();
    public bool IsDisposed { get; private set; }
    ~IocContainerScope() => Dispose(false);
    private void Dispose(bool disposing)
    {
        // ReleaseUnmanagedResourcesIfSomeGetAdded:
        // here
        
        if (!disposing) return;
        LockScopedContainer();
        // sync dispose:
        SyncDispose();
    }
    
    public void Dispose()
    {
        if (IsDisposed) return;
        lock (_mutex)
        {
            if (IsDisposed) return;
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
        // sync dispose:
        Dispose(true);
    }
    public ValueTask DisposeAsync()
    {
        if (IsDisposed) return default;
        lock (_mutex)
        {
            if (IsDisposed) return default;
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
        // sync common and or unmanaged dispose (without executing SyncDispose()):
        Dispose(false);
        LockScopedContainer();
        // async dispose:
        return AsyncDispose();
    }

    private void LockScopedContainer()
    {
        // todo Lock scoped Container in the sense of - no more service resolvation possible, don't affect root provider
    }
}