using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Binkus.DependencyInjection;

public sealed class IocContainer : IServiceProvider, IIocContainerBuilder
{
    private static IEnumerable<KeyValuePair<Type, IocDescriptor>> ToKeyValuePair(IEnumerable<IocDescriptor> services) 
        => services.Select(d => new KeyValuePair<Type, IocDescriptor>(d.ServiceType, d));
    
    private IIocContainerProvider CreateContainerProvider(IEnumerable<IocDescriptor> services) 
        => IsReadOnly
            ? new FrozenConcurrentIocContainerProvider(
                new ConcurrentDictionary<Type, IocDescriptor>(
                    ToKeyValuePair(services)))
            : new MutableConcurrentIocContainerProvider(
                new ConcurrentDictionary<Type, IocDescriptor>(
                    ToKeyValuePair(services))); 

    public IocContainer(IEnumerable<IocDescriptor> services, bool readOnly = true) : this(services, null, readOnly) { }
    public IocContainer(IEnumerable<IocDescriptor> services, ServiceScopeId? id, bool readOnly = true) : this(readOnly)
    {
        var rootContainerProvider = CreateContainerProvider(services);
        RootContainerScope = new IocContainerScope(rootContainerProvider, id);
    }
    public IocContainer(ServiceScopeId? id = null) : this(false)
    {
        var rootContainerProvider =
            new MutableConcurrentIocContainerProvider(
                new ConcurrentDictionary<Type, IocDescriptor>());
        RootContainerScope = new IocContainerScope(rootContainerProvider, id);
    }

#nullable disable
    private IocContainer(bool readOnly)
    {
        IsReadOnly = readOnly;
    }
#nullable enable

    public bool IsReadOnly { get; init; }
    // internal IIocContainerProvider RootContainerProvider { get; }
    public IocContainerScope RootContainerScope { get; }

    public ConcurrentDictionary<ServiceScopeId, IocContainerScope> Scopes { get; } = new();

    public object? GetService(Type serviceType) => RootContainerScope.RootContainerScope.ContainerProvider.GetService(serviceType);

    public IocContainerScope CreateScope(IocContainerScope root, IocContainerScope parent)
    {
        // todo cache
        var scopedDescriptors = RootContainerScope.RootContainerScope.ContainerProvider.Descriptors.Values.Where(d => d.Lifetime is IocLifetime.Scoped);
        var container = CreateContainerProvider(scopedDescriptors);
        var scope = new IocContainerScope(root, parent, container);
        Scopes.TryAdd(scope.Id, scope);
        return scope;
    }

    public IIocContainerBuilder Add(IocDescriptor descriptor)
    {
        var descriptors = RootContainerScope.RootContainerScope.ContainerProvider.Descriptors as ConcurrentDictionary<Type, IocDescriptor>;
        if (descriptors is not null) Add(descriptors, descriptor);
        if (descriptor.Lifetime is not IocLifetime.Scoped) return this;
        foreach (var iocContainerScope in Scopes.Values)
        {
            descriptors = iocContainerScope.ContainerProvider.Descriptors as ConcurrentDictionary<Type, IocDescriptor>;
            if (descriptors is null) continue;
            Add(descriptors, descriptor.CreateForScope());
        }
        return this;
    }

    private void Add(ConcurrentDictionary<Type, IocDescriptor> descriptors, IocDescriptor descriptorItemToAdd, IocDescriptor? prev = null)
    {
        if(prev is null) descriptors.TryGetValue(descriptorItemToAdd.ServiceType, out prev);
        while (prev?.Next is not null)
        {
            Add(descriptors, descriptorItemToAdd, prev.Next);
        }
        if (!descriptors.TryAdd(descriptorItemToAdd.ServiceType, descriptorItemToAdd))
        {
            prev!.Next = descriptorItemToAdd;
        }
        
    }

    public IocContainer Build() => new(RootContainerScope.ContainerProvider.Descriptors.Values);

    // public IocContainer MarkAsReadOnly() => IsReadOnly = true;
}

public interface IIocContainerBuilder
{
    IIocContainerBuilder Add(IocDescriptor descriptor);
    IocContainer Build();
}

// IIocContainerProvider

public interface IIocContainerProvider : IServiceProvider
{
    public IocContainerScope ContainerScope { get; internal set; }
    internal IReadOnlyDictionary<Type, IocDescriptor> Descriptors { get; }

    public ReadOnlyDictionary<Type, IocDescriptor> GetDescriptorsCopy => new((IDictionary<Type, IocDescriptor>)Descriptors);

    protected object? GetSpecialService(Type serviceType)
    {
        Type[]? generics;
        if (serviceType.ContainsGenericParameters && (generics = serviceType.GetGenericArguments()).Length == 1)
        {
            if (serviceType == typeof(IEnumerable<>).MakeGenericType(generics))
            {
                
            }
            else if (serviceType == typeof(Lazy<>).MakeGenericType(generics))
            {
                
            }
        }

        return null;
    }
}
    
internal sealed record FrozenConcurrentIocContainerProvider(IReadOnlyDictionary<Type, IocDescriptor> Descriptors)
    : IIocContainerProvider
{
    IocContainerScope IIocContainerProvider.ContainerScope { get; set; } = null!;
    public IocContainerScope ContainerScope => ((IIocContainerProvider)this).ContainerScope; 
    
    public object? GetService(Type serviceType)
    {
        if (ContainerScope.RootContainerScope.ContainerProvider.Descriptors
            .TryGetValue(serviceType, out var descriptor))
        {
            if (descriptor.Lifetime is IocLifetime.Scoped)
                return Descriptors.TryGetValue(serviceType, out var scopedDescriptor)
                    ? ContainerScope.GetServiceForDescriptor(scopedDescriptor)
                    : null;
            return ContainerScope.GetServiceForDescriptor(descriptor);
        }
        return null;
    }
}

internal sealed record MutableConcurrentIocContainerProvider(ConcurrentDictionary<Type, IocDescriptor> Descriptors)
    : IIocContainerProvider
{
    IReadOnlyDictionary<Type, IocDescriptor> IIocContainerProvider.Descriptors => Descriptors;

    IocContainerScope IIocContainerProvider.ContainerScope { get; set; } = null!;
    public IocContainerScope ContainerScope => ((IIocContainerProvider)this).ContainerScope; 
        
    public object? GetService(Type serviceType)
    {
        if (ContainerScope.RootContainerScope.ContainerProvider.Descriptors
            .TryGetValue(serviceType, out var descriptor))
        {
            if (descriptor.Lifetime is IocLifetime.Scoped)
                return Descriptors.TryGetValue(serviceType, out var scopedDescriptor)
                    ? ContainerScope.GetServiceForDescriptor(scopedDescriptor)
                    : null;
            return ContainerScope.GetServiceForDescriptor(descriptor);
        }
        return null;
    }
}

// Scope Engine

public sealed record IocContainerScope : IDisposable, IAsyncDisposable, IServiceProvider
{
    public IocContainerScope RootContainerScope { get; init; }
    public IocContainerScope ParentContainerScope { get; init; }
    public IIocContainerProvider ContainerProvider { get; init; }
    public ServiceScopeId Id { get; init; }
    public bool IsRootContainerScope { get; init; }

    internal IocContainerScope(IIocContainerProvider containerProvider, ServiceScopeId? id = null)
    {
        RootContainerScope = this;
        ParentContainerScope = this;
        ContainerProvider = containerProvider;
        ContainerProvider.ContainerScope = this;
        Id = id ?? new ServiceScopeId();
        IsRootContainerScope = true;
    }
    
    internal IocContainerScope(IocContainerScope rootContainerScope, IocContainerScope parentContainerScope, IIocContainerProvider containerProvider, ServiceScopeId? id = null)
    {
        RootContainerScope = rootContainerScope;
        ParentContainerScope = parentContainerScope;
        ContainerProvider = containerProvider;
        ContainerProvider.ContainerScope = this;
        Id = id ?? new ServiceScopeId();
        IsRootContainerScope = false;
    }
    
    public object? GetService(Type serviceType) => ContainerProvider.GetService(serviceType);

    internal object? GetServiceForDescriptor(IocDescriptor descriptor)
    {
        while (descriptor.Next is not null)
        {
            return GetServiceForDescriptor(descriptor);
        }
        return GetServiceForDescriptor2(descriptor);
    }
    
    private object? GetServiceForDescriptor2(IocDescriptor descriptor)
        => descriptor.Lifetime switch
        {
            IocLifetime.Singleton => GetSingletonService(descriptor),
            IocLifetime.Scoped => GetScopedService(descriptor),
            IocLifetime.Transient => GetTransientService(descriptor),
            _ => null
        } ?? GetServiceForDescriptor3(descriptor);

    private object? GetServiceForDescriptor3(IocDescriptor descriptor)
    {
        
        
        return null;
    }

    internal object? GetSingletonService(IocDescriptor descriptor)
    {
        if (descriptor.Implementation is { }) return descriptor.Implementation;
        if (descriptor.Factory?.Invoke(ContainerProvider) is { } serviceImpl) return serviceImpl;
        lock (descriptor)
        {
            return descriptor.Implementation ??= TryCreateService(ContainerProvider, descriptor);    
        }
    }

    internal object? GetScopedService(IocDescriptor descriptor) => GetSingletonService(descriptor); // it is scoped already
    
    public object? GetTransientService(IocDescriptor descriptor)
    {
        if (descriptor.Factory?.Invoke(ContainerProvider) is { } serviceImpl) return serviceImpl;
        return TryCreateService(ContainerProvider, descriptor);
    }

    public static object? TryCreateService(IServiceProvider provider, IocDescriptor descriptor)
    {
        var type = descriptor.ImplType ?? descriptor.ServiceType;
        if (type.IsAbstract) return null;

        return IocUtilities.CreateInstance(provider, type);

        // return Activator.CreateInstance(type);
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
            if (!IsRootContainerScope && descriptor.Lifetime is not IocLifetime.Scoped) continue;
            // only scoped services get disposed as well as singletons if this represents the root IoC Container:
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
            if (!IsRootContainerScope && descriptor.Lifetime is not IocLifetime.Scoped) continue;
            // only scoped services get disposed as well as singletons if this represents the root IoC Container:
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