using System.Collections.Concurrent;
using System.Collections.ObjectModel;

// ReSharper disable MemberCanBePrivate.Global

namespace Binkus.DependencyInjection;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class IocContainer : IServiceProvider
{
    public bool IsReadOnly { get; init; }

    // public IocContainer(
    //     IEnumerable<
    //             (IocLifetime Lifetime, Type ServiceType,Type? ImplType, object? Implementation, ServiceFactory? Factory)
    //         > services, bool readOnly = true)
    //     : this(services.Select(d => 
    //         new IocDescriptor
    //         {
    //             Lifetime = d.Lifetime,
    //             ServiceType = d.ServiceType,
    //             ImplType = d.ImplType,
    //             Implementation = d.Implementation,
    //             Factory = d.Factory
    //         }),
    //         readOnly) { }

    private static IEnumerable<KeyValuePair<Type, IocDescriptor>> ToKeyValuePair(IEnumerable<IocDescriptor> services) 
        => services.Select(d => new KeyValuePair<Type, IocDescriptor>(d.ServiceType, d));

    public IocContainer(IEnumerable<IocDescriptor> services, bool readOnly = true) : this(readOnly)
    {
        ContainerProvider = readOnly
            ? new FrozenConcurrentIocContainerProvider(this,
                new ConcurrentDictionary<Type, IocDescriptor>(
                    ToKeyValuePair(services)))
            : new MutableConcurrentIocContainerProvider(this,
                new ConcurrentDictionary<Type, IocDescriptor>(
                    ToKeyValuePair(services)));
        RootScope = new IocContainerScope(ContainerProvider, true);
    }

    public IocContainer() : this(false)
    {
        ContainerProvider =
            new MutableConcurrentIocContainerProvider(this,
                new ConcurrentDictionary<Type, IocDescriptor>());
        RootScope = new IocContainerScope(ContainerProvider, true);
    }

#nullable disable
    private IocContainer(bool readOnly)
    {
        IsReadOnly = readOnly;
    }
#nullable enable

    internal IIocContainerProvider ContainerProvider { get; }
    public IocContainerScope RootScope { get; }

    public object? GetService(Type serviceType) => ContainerProvider.GetService(serviceType);

    public interface IIocContainerProvider : IServiceProvider
    {
        internal IReadOnlyDictionary<Type, IocDescriptor> Descriptors { get; }

        public ReadOnlyDictionary<Type, IocDescriptor> GetDescriptorsCopy => new((IDictionary<Type, IocDescriptor>)Descriptors);
    }
    
    private sealed record FrozenConcurrentIocContainerProvider(IocContainer Container, IReadOnlyDictionary<Type, IocDescriptor> Descriptors)
        : IIocContainerProvider
    {
        public object? GetService(Type serviceType) 
            => Descriptors.TryGetValue(serviceType, out var descriptor) ? Container.RootScope.GetServiceForDescriptor(descriptor) : null;
    }

    private sealed record MutableConcurrentIocContainerProvider(IocContainer Container, ConcurrentDictionary<Type, IocDescriptor> Descriptors)
        : IIocContainerProvider
    {
        IReadOnlyDictionary<Type, IocDescriptor> IIocContainerProvider.Descriptors => Descriptors;
        
        public object? GetService(Type serviceType) 
            => Descriptors.TryGetValue(serviceType, out var descriptor) ? Container.RootScope.GetServiceForDescriptor(descriptor) : null;
    }

    public IocContainerScope CreateScope()
    {
        return new IocContainerScope(ContainerProvider);
    }
}

public sealed record IocContainerScope(IocContainer.IIocContainerProvider ContainerProvider, ServiceScopeId Id, bool IsRootScope) : IDisposable, IAsyncDisposable
{
    internal IocContainerScope(IocContainer.IIocContainerProvider containerProvider, bool isRootScope = false) : this(containerProvider, new ServiceScopeId(), isRootScope) {}
    internal IocContainerScope(IocContainer.IIocContainerProvider containerProvider, ServiceScopeId? id) : this(containerProvider, id ?? new ServiceScopeId(), false) {}
    
    public object? GetServiceForDescriptor(IocDescriptor descriptor)
        => descriptor.Lifetime switch
        {
            IocLifetime.Singleton => GetSingletonService(descriptor),
            IocLifetime.Scoped => GetScopedService(descriptor),
            IocLifetime.Transient => GetTransientService(descriptor),
            _ => null
        };

    public object? GetSingletonService(IocDescriptor descriptor)
    {
        if (descriptor.Implementation is { }) return descriptor.Implementation;
        if (descriptor.Factory?.Invoke(ContainerProvider) is { } serviceImpl) return serviceImpl;
        lock (descriptor)
        {
            return descriptor.Implementation ??= TryCreateService(descriptor);    
        }
    }
    
    public object? GetScopedService(IocDescriptor descriptor)
    {
        if (descriptor.Implementation is { }) return descriptor.Implementation;
        if (descriptor.Factory?.Invoke(ContainerProvider) is { } serviceImpl) return serviceImpl;
        lock (descriptor)
        {
            return descriptor.Implementation ??= TryCreateService(descriptor);    
        }
    }
    
    public static object? GetTransientService(IocDescriptor descriptor) => TryCreateService(descriptor);

    public static object? TryCreateService(IocDescriptor descriptor)
    {
        var type = descriptor.ImplType ?? descriptor.ServiceType;
        if (type.IsAbstract) return null;
        return Activator.CreateInstance(type);
    }
    
    //
    
    public bool IsDisposed { get; private set; }
    ~IocContainerScope() => Dispose(false);
    private void Dispose(bool disposing)
    {
        // ReleaseUnmanagedResourcesIfSomeGetAdded:
        // here
        
        // todo Lock scoped Container in the sense of - no more service resolvation possible, don't affect root provider
        if (!disposing) return;
        // sync dispose:
        IEnumerable<IocDescriptor> descriptors = ContainerProvider.Descriptors.Values;
        foreach (var descriptor in descriptors)
        {
            if (!IsRootScope && descriptor.Lifetime is not IocLifetime.Scoped) continue;
            switch (descriptor.Implementation)
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
    public void Dispose()
    {
        if (IsDisposed) return;
        Dispose(true);
        GC.SuppressFinalize(this);
        IsDisposed = true;
    }
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed) return;
        Dispose(false);
        // async dispose:
        IEnumerable<IocDescriptor> descriptors = ContainerProvider.Descriptors.Values;
        foreach (var descriptor in descriptors)
        {
            if (!IsRootScope && descriptor.Lifetime is not IocLifetime.Scoped) continue;
            switch (descriptor.Implementation)
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
        GC.SuppressFinalize(this);
        IsDisposed = true;
    }
}

public sealed record ServiceScopeId(Guid Id)
{
    public ServiceScopeId() : this(Guid.NewGuid()) { }
    
    public static readonly ServiceScopeId Empty = new(Guid.Empty); 
    public static implicit operator ServiceScopeId(Guid id) => new(id);
    public static implicit operator Guid(ServiceScopeId id) => id.Id;
}