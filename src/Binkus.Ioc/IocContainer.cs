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
        ContainerScope = RootContainerScope = new IocContainerScope(services, id);
    }

    public IocContainer(ServiceScopeId? id = null) : this(false)
    {
        ContainerScope = RootContainerScope = new IocContainerScope(id);
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

internal record ServiceImpls : IEnumerable<ServiceImpls>
{
    // // ReSharper disable once ConvertToPrimaryConstructor
    // public ServiceImpls(IocDescriptor descriptor)
    // {
    //     Descriptor = descriptor;
    //     Item = descriptor.Implementation;
    // }
    
    // ReSharper disable once ConvertToPrimaryConstructor
    public ServiceImpls(object? item = null) => Item = item;

    // public IocDescriptor Descriptor { get; init; }
    public object? Item { get; internal set; }
    public ServiceImpls? Next { get; internal set; }
    
    public ServiceImplEnumerator GetEnumerator() => new ServiceImplEnumerator(this);
    IEnumerator<ServiceImpls> IEnumerable<ServiceImpls>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public record struct ServiceImplEnumerator(ServiceImpls First) : IEnumerator<ServiceImpls>
    {
        public bool MoveNext()
        {
            if (Current is null)
            {
                Current = First;
                return true;
            }
            if (Current.Next is null) return false;
            Current = Current.Next;
            return true;
        }

        public void Reset() => Current = null!;
        public ServiceImpls Current { get; private set; }
        object IEnumerator.Current => Current;
        public void Dispose() { }
    }
    
    internal ServiceImpls Prepend(ServiceImpls itemToAdd)
    {
        if (itemToAdd == this) return itemToAdd;
        itemToAdd.Next = this;
        return itemToAdd;
    }

    internal ServiceImpls Add(ServiceImpls itemToAdd)
    {
        var last = this.LastOrDefault(x => x != itemToAdd);
        if (last != null)
            last.Next = itemToAdd;
        return this;
    }
}

internal record ServiceInstanceProvider(object? Instance = null)
{
    public object? Instance { get; internal set; } = Instance;
}

// Scope Engine

public sealed record IocContainerScope : IDisposable, IAsyncDisposable, IServiceProvider
{
    public IocContainerScope RootContainerScope { get; init; }
    public IocContainerScope ParentContainerScope { get; init; }
    public ServiceScopeId Id { get; init; }
    public bool IsRootContainerScope { get; init; }

    private static IEnumerable<KeyValuePair<Type, IocDescriptor>> ToKeyValuePair(IEnumerable<IocDescriptor> services) 
        => services.Select(d => new KeyValuePair<Type, IocDescriptor>(d.ServiceType, d));
    
    // private static IEnumerable<KeyValuePair<Type, IocDescriptor>> PreserveLastDuplicate(
    //     IEnumerable<KeyValuePair<Type, IocDescriptor>> collection) =>
    //     collection.skip;

    public IocContainerScope() : this(default(IEnumerable<IocDescriptor>), default(ServiceScopeId)) { }
    internal IocContainerScope(ServiceScopeId? id) : this(default(IEnumerable<IocDescriptor>), id) { }
    public IocContainerScope(IEnumerable<IocDescriptor> services) : this(services, default(ServiceScopeId)) { }
    internal IocContainerScope(IEnumerable<IocDescriptor>? services, ServiceScopeId? id)
    {
        RootContainerScope = this;
        ParentContainerScope = this;
        Id = id ?? new ServiceScopeId();
        IsRootContainerScope = true;
        Descriptors = services?.ToList() ?? new List<IocDescriptor>();
        CachedDescriptors = new ConcurrentDictionary<Type, IocDescriptor>();
        ScopedDescriptors = Descriptors.Where(d => d.Lifetime is IocLifetime.Scoped).ToList();
        // CachedDescriptors = services is null
        //     ? new ConcurrentDictionary<Type, IocDescriptor>()
        //     : new ConcurrentDictionary<Type, IocDescriptor>(ToKeyValuePair(Descriptors));

        Singletons = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>();
        Scoped = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>();
        // Singletons = new ConcurrentDictionary<Type, ServiceImpls>();
        // Scoped = new ConcurrentDictionary<Type, ServiceImpls>();
        
        if (services == null) return;

        foreach (var descriptor in Descriptors)
        {
            // Newer descriptors (farther in the list) replace old ones when ServiceType is equal
            CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
                static (_, d) => d, 
                static (_, _, d) => d,
                descriptor);
        }
        
        InternalAddSingletons();
        AddBasicServices();
    }

    private void AddBasicServices()
    {
        var d = IocDescriptor.CreateTransient(_ => IocUtilitiesDelegation.Default);
        if (!CachedDescriptors.TryAdd(typeof(IocUtilitiesDelegation), d)) return;
        Descriptors.Add(d);
        // Singletons.TryAdd(d, new ServiceInstanceProvider(d.Implementation));
    }
    
    // Creates Scope
    private IocContainerScope(IocContainerScope parentContainerScope)
    {
        RootContainerScope = parentContainerScope.RootContainerScope;
        ParentContainerScope = parentContainerScope;
        Id = new ServiceScopeId();
        IsRootContainerScope = false;
        Singletons = RootContainerScope.Singletons;
        Scoped = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>();
        // Scoped = new ConcurrentDictionary<Type, ServiceImpls>();
        Descriptors = RootContainerScope.Descriptors;
        CachedDescriptors = RootContainerScope.CachedDescriptors;
        ScopedDescriptors = RootContainerScope.ScopedDescriptors;
        
        InternalAddScoped();
    }
    
    public interface IAmUnitTesting
    {
        public static IocContainerScope CreateScope(IocContainerScope parentContainerScope, ServiceScopeId? id = null)
            => id is null
                ? new(parentContainerScope)
                : new(parentContainerScope) { Id = id };
    }
    internal IocContainerScope CreateScope(ServiceScopeId id) => new(this) { Id = id };
    
    public IocContainerScope CreateScope() => new(this);
    

    // descriptors should be a list to allow duplicates
    private List<IocDescriptor> Descriptors { get; init; }
    private List<IocDescriptor> ScopedDescriptors { get; init; }
    // private ConcurrentDictionary<Type, IocDescriptor> Descriptors { get; }
    
    // private ConcurrentDictionary<Type, ServiceImpls> Singletons { get; }
    // private ConcurrentDictionary<Type, ServiceImpls> Scoped { get; }
    
    private ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> Singletons { get; }
    private ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> Scoped { get; }
    
    private ConcurrentDictionary<Type, IocDescriptor> CachedDescriptors { get; }


    private void InternalAddSingletons()
    {
        foreach (var descriptor in Descriptors.Where(d => d.Lifetime is IocLifetime.Singleton)) 
            InternalAddServiceImpls(Singletons, descriptor);
    }
    
    private void InternalAddScoped()
    {
        foreach (var descriptor in Descriptors.Where(d => d.Lifetime is IocLifetime.Scoped)) 
            InternalAddServiceImpls(Scoped, descriptor);
    }
    
    private static void InternalAddServiceImpls(ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> dict, IocDescriptor descriptor)
    {
        dict.TryAdd(descriptor, new ServiceInstanceProvider(descriptor.Implementation));
        
        // dict.AddOrUpdate(descriptor, static (_, d) => new ServiceInstanceProvider(d.Implementation),
        //     static (_, s, d) => new ServiceInstanceProvider(d.Implementation),
        //     descriptor);

        // dict.AddOrUpdate(descriptor.ServiceType, static (_, d) => new ServiceImpls(d),
        //     static (_, s, d) => s.Prepend(new ServiceImpls(d)), descriptor);
    }
    

    // public void Add2(IocDescriptor descriptor)
    // {
    //     Descriptors.Add(descriptor);
    //     AddInternal2(descriptor);
    // }
    //
    // // set singletons from descriptors Descriptor.Implementation
    // private void AddInternal2(IocDescriptor descriptor, bool needsLock = true)
    // {
    //     var dict = descriptor.Lifetime == IocLifetime.Singleton
    //         ? Singletons
    //         : Scoped;
    //     if (descriptor.Implementation is null || descriptor.Lifetime is IocLifetime.Transient ||
    //         !dict.ContainsKey(descriptor.ServiceType) && dict.TryAdd(descriptor.ServiceType,
    //             new ServiceImpls(descriptor.Implementation)) ||
    //         !dict.TryGetValue(descriptor.ServiceType, out var serviceImpls)) return;
    //     if (!needsLock)
    //     {
    //         AddNext();
    //         return;
    //     }
    //     lock (serviceImpls) AddNext();
    //     void AddNext() => serviceImpls.Last().Next = new ServiceImpls(descriptor.Implementation);
    // }

    public object? GetService(Type serviceType)
    {
        return 
            CachedDescriptors.TryGetValue(serviceType, out var descriptor)
            ? GetServiceForDescriptor2(descriptor)
            : null;
        // return GetServiceForDescriptor2(Descriptors.Last(x => x.ServiceType == serviceType));
    }

    private object? GetSpecialService(Type serviceType)
    {
        bool isOpenGeneric = serviceType.IsGenericTypeDefinition;
        return null;
    }

    private void SpecialService(IocDescriptor descriptor)
    {
        
    }
    
    private object? GetServiceForDescriptor2(IocDescriptor descriptor)
        => descriptor.Lifetime switch
        {
            IocLifetime.Singleton => GetSingletonOrScopedService(Singletons, descriptor),
            IocLifetime.Scoped => GetSingletonOrScopedService(Scoped, descriptor),
            IocLifetime.Transient => GetTransientService(descriptor),
            _ => null
        };
    
    internal object? GetSingletonOrScopedService(ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> dict, IocDescriptor descriptor)
    {
        if (!dict.TryGetValue(descriptor, out var instanceProvider)) return null;
        
        return instanceProvider.Instance ?? Locke();
            
        object? Locke()
        {
            lock (instanceProvider)
                return instanceProvider.Instance ??=
                    descriptor.Factory?.Invoke(this) ?? TryCreateService(this, descriptor);
        }
    }

    // internal object? GetSingletons(IocDescriptor descriptor)
    // {
    //     if (!Singletons.TryGetValue(descriptor.ServiceType, out var s)) return null;
    //     
    //     return s.Item ?? Locke();
    //         
    //     object? Locke()
    //     {
    //         lock (s) return s.Item ??= descriptor.Factory?.Invoke(this);
    //     }
    // }
    
    // internal object? GetLastSingletons(IocDescriptor descriptor)
    // {
    //     if (!Singletons.TryGetValue(descriptor.ServiceType, out var s)) return null;
    //     
    //     var last = s.Last();
    //     
    //     return last.Item ?? Locke();
    //         
    //     object? Locke()
    //     {
    //         lock (s) return last.Item ??= descriptor.Factory?.Invoke(this);
    //     }
    // }

    // internal object? GetSingletonOrScopedService(IocDescriptor descriptor)
    // {
    //     if (descriptor.Implementation is { }) return descriptor.Implementation;
    //     
    //     
    //     
    //     // if (descriptor.Factory?.Invoke(ContainerProvider) is { } serviceImpl) return serviceImpl;
    //     // lock (descriptor)
    //     // {
    //     //     return descriptor.Implementation ??= TryCreateService(ContainerProvider, descriptor);    
    //     // }
    //
    //     return null;
    // }

    // internal object? GetScopedService(IocDescriptor descriptor) => GetSingletonService(descriptor); // it is scoped already
    
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

        return IocUtilitiesDelegation.Default.CreateInstance(provider, type);

        // return Activator.CreateInstance(type);
    }
    
    internal enum ActionType
    {
        Invalid,
        CachedInstance,
        ImplType,
        Factory,
    }

    internal ActionType GetDescriptorActionType(IocDescriptor descriptor)
        => descriptor switch
        {
            { Implementation: { } } => ActionType.CachedInstance,
            { Factory: { } } => ActionType.Factory,
            { ImplType: { } } => ActionType.ImplType,
            _ => ActionType.Invalid
        };
    
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

