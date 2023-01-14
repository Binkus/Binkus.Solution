using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Binkus.DependencyInjection.Extensions;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Binkus.DependencyInjection;

public sealed class IocContainer // : IServiceProvider
{
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

public interface IContainerScopeFactory
{
    IContainerScope CreateScope();
}

public interface IContainerScope : IContainerScopeFactory, IAsyncDisposable, IDisposable
{
    public IServiceProvider Services { get; }
}


internal record ServiceInstanceProvider(object? Instance = null)
{
    public object? Instance { get; internal set; } = Instance;
}

// Scope Engine

public sealed record IocContainerScope : IServiceProvider, IContainerScope,
    IContainerScopeFactory, IDisposable, IAsyncDisposable, IEnumerable<IocDescriptor>
{
    public List<IocDescriptor>.Enumerator GetEnumerator()
    {
        lock (Descriptors) return Descriptors.ToList().GetEnumerator();
    }
    
    IEnumerator<IocDescriptor> IEnumerable<IocDescriptor>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
        // return ((IEnumerable)Descriptors).GetEnumerator();
        // lock (Descriptors)
        // {
        //     var d = new IocDescriptor[Descriptors.Count];
        //     Descriptors.CopyTo(d);
        //     return d.AsEnumerable().GetEnumerator();            
        // }
    }

    private readonly IServiceProvider? _wrappedProvider;
    public IServiceProvider? WrappedProvider { get => _wrappedProvider; init => _wrappedProvider = Equals(value, this) ? _wrappedProvider : value; }
    public IServiceProvider Services => this;
    public IocContainerScope RootContainerScope { get; }
    public IocContainerScope? ParentContainerScope => WeakParentContainerScope?.TryGetTarget(out var target) ?? false ? target : null;
    private WeakReference<IocContainerScope>? WeakParentContainerScope { get; }
    public ServiceScopeId Id { get; init; }
    public bool IsRootContainerScope { get; init; }

    private static IEnumerable<KeyValuePair<Type, IocDescriptor>> ToKeyValuePair(IEnumerable<IocDescriptor> services) 
        => services.Select(d => new KeyValuePair<Type, IocDescriptor>(d.ServiceType, d));

    public IocContainerScope() : this(default(IEnumerable<IocDescriptor>), default(ServiceScopeId)) { }
    internal IocContainerScope(ServiceScopeId? id) : this(default(IEnumerable<IocDescriptor>), id) { }
    public IocContainerScope(IEnumerable<IocDescriptor> services) : this(services, default(ServiceScopeId)) { }
    internal IocContainerScope(IEnumerable<IocDescriptor>? services, ServiceScopeId? id)
    {
        // WrappedProvider ...
        RootContainerScope = this;
        WeakParentContainerScope = null;
        Id = id ?? new ServiceScopeId();
        IsRootContainerScope = true;
        Descriptors = services?.ToList() ?? new List<IocDescriptor>();
        CachedDescriptors = new ConcurrentDictionary<Type, IocDescriptor>();
        ScopedDescriptors = Descriptors.Where(d => d.Lifetime is IocLifetime.Scoped).ToList();
        Singletons = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>();
        Scoped = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>();
        
        if (services == null) return;

        foreach (var descriptor in Descriptors)
        {
            // Newer descriptors (farther in the list) replace old ones when ServiceType is equal
            CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
                static (_, d) => d, 
                static (_, _, d) => d,
                descriptor);
        }
        
        InternalAddThisAsService();
        InternalAddSingletons();
        InternalAddScoped();
        AddBasicServices();
    }

    private void AddBasicServices()
    {
        var type = typeof(IocUtilitiesDelegation);
        if (CachedDescriptors.ContainsKey(type)) return;
        var d = IocDescriptor.CreateSingleton(IocUtilitiesDelegation.NewUninitializedIocUtilitiesDelegation());
        if (!CachedDescriptors.TryAdd(type, d)) return;
        Descriptors.Add(d);
        Singletons.TryAdd(d, new ServiceInstanceProvider(d.Implementation));
    }
    
    private void InternalAddThisAsService()
    {
        // var d1 = IocDescriptor.CreateScoped(this);
        var d1 = IocDescriptor.CreateScoped(p => p);
        var d2 = IocDescriptor.CreateScoped<IocContainerScope>(p => (IocContainerScope)p);
        
        Scoped.TryAdd(d1, new ServiceInstanceProvider());
        Scoped.TryAdd(d2, new ServiceInstanceProvider());
        CachedDescriptors.TryAdd(d1.ServiceType, d1);
        CachedDescriptors.TryAdd(d2.ServiceType, d2);
        
        Descriptors.Add(d1);
        Descriptors.Add(d2);
    }
    
    // Creates Scope
    private IocContainerScope(IocContainerScope parentContainerScope)
    {
        _wrappedProvider =
            parentContainerScope._wrappedProvider?.GetService<IContainerScopeFactory>()?.CreateScope().Services ??
            parentContainerScope._wrappedProvider; // create scope from wrapped provider
        RootContainerScope = parentContainerScope.RootContainerScope;
        WeakParentContainerScope = new WeakReference<IocContainerScope>(parentContainerScope);
        Id = new ServiceScopeId();
        IsRootContainerScope = false;
        Descriptors = RootContainerScope.Descriptors;
        CachedDescriptors = RootContainerScope.CachedDescriptors;
        ScopedDescriptors = RootContainerScope.ScopedDescriptors;
        Singletons = RootContainerScope.Singletons;
        Scoped = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>();

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
    
    IContainerScope IContainerScopeFactory.CreateScope() => CreateScope();
    public IocContainerScope CreateScope() => new(this);
    
    private List<IocDescriptor> Descriptors { get; init; }
    private List<IocDescriptor> ScopedDescriptors { get; init; }
    
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
        foreach (var descriptor in ScopedDescriptors) 
            InternalAddServiceImpls(Scoped, descriptor);
    }
    
    private static void InternalAddServiceImpls(ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> dict, IocDescriptor descriptor)
    {
        dict.TryAdd(descriptor, new ServiceInstanceProvider(descriptor.Implementation));
        // dict.AddOrUpdate(descriptor, static (_, d) => new ServiceInstanceProvider(d.Implementation),
        //     static (_, s, d) => new ServiceInstanceProvider(d.Implementation),
        //     descriptor);
    }
    
    public bool TryAdd(IocDescriptor descriptor) => TryAdd(descriptor, true);
    public void Add(IocDescriptor descriptor) => Add(descriptor, true);
    private bool Add(IocDescriptor descriptor, bool lockDescriptors)
    {
        if (!lockDescriptors)
            return InternalUnsafeAdd(descriptor);
        lock (Descriptors)
            return InternalUnsafeAdd(descriptor);
    }

    private bool InternalUnsafeAdd(IocDescriptor descriptor)
    {
        // if (IsReadOnly) return false;
        CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
            static (_, d) => d, static (_, _, d) => d, descriptor);
        Descriptors.Add(descriptor);
        if (descriptor.Lifetime is IocLifetime.Transient) return true;
        if (descriptor.Lifetime is IocLifetime.Scoped) ScopedDescriptors.Add(descriptor);
        var dict =
            descriptor.Lifetime is IocLifetime.Scoped ? Scoped : Singletons;
        dict.AddOrUpdate(descriptor, static (_, d) => new ServiceInstanceProvider(d.Implementation),
            static (_, s, d) =>
            {
                s.Instance = d.Implementation;
                return s;
            }, descriptor);
        WhenDisposedDisposeImplFrom(descriptor);
        return true;
    }

    private bool TryAdd(IocDescriptor descriptor, bool lockDescriptors)
    {
        if (CachedDescriptors.ContainsKey(descriptor.ServiceType)) return false;
        if (!lockDescriptors)
            return InternalUnsafeInnerLockTryAdd(descriptor);
        lock (Descriptors)
            return InternalUnsafeInnerLockTryAdd(descriptor);
    }

    private bool InternalUnsafeInnerLockTryAdd(IocDescriptor descriptor)
    {
        // if (IsReadOnly) return false;
        if (!CachedDescriptors.TryAdd(descriptor.ServiceType, descriptor)) return false;
        Descriptors.Add(descriptor);
        if (descriptor.Lifetime is IocLifetime.Transient) return true;
        if (descriptor.Lifetime is IocLifetime.Scoped) ScopedDescriptors.Add(descriptor);
        var dict = 
            descriptor.Lifetime is IocLifetime.Scoped ? Scoped : Singletons;
        dict.TryAdd(descriptor, new ServiceInstanceProvider(descriptor.Implementation));
        WhenDisposedDisposeImplFrom(descriptor);
        return true;
    }

    public object? GetService(Type serviceType)
    {
        return 
            CachedDescriptors.TryGetValue(serviceType, out var descriptor)
            ? GetServiceForDescriptor(descriptor)
            : WrappedProvider?.GetService(serviceType);
    }

    private object? GetSpecialService(Type serviceType)
    {
        bool isOpenGeneric = serviceType.IsGenericTypeDefinition;
        return null;
    }

    private void SpecialService(IocDescriptor descriptor)
    {
        
    }
    
    private object? GetServiceForDescriptor(IocDescriptor descriptor)
        => descriptor.Lifetime switch
        {
            IocLifetime.Singleton => GetSingletonOrScopedService(Singletons, descriptor),
            IocLifetime.Scoped => IsDisposed && !IsRootContainerScope 
                ? throw new InvalidOperationException("Scope is disposed.")
                : GetSingletonOrScopedService(Scoped, descriptor),
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
    
    internal object? GetTransientService(IocDescriptor descriptor)
    {
        if (descriptor.Factory?.Invoke(this) is { } serviceInstance) return serviceInstance;
        return TryCreateService(this, descriptor);
        // return null;
    }

    internal static object? TryCreateService(IServiceProvider provider, IocDescriptor descriptor)
    {
        if (descriptor.ServiceType.IsGenericTypeDefinition)
        {
            // open generic
            return null;
        }
        var type = descriptor.ImplType ?? descriptor.ServiceType;
        return type.IsAbstract
            ? null
            : provider.GetRequiredService<IocUtilitiesDelegation>().CreateInstance(provider, type);
        // return provider.TryCreateInstance(type);
        // return IocUtilitiesDelegation.Default.CreateInstance(provider, type);
    }
    
    internal enum ActionType
    {
        Invalid,
        CachedInstance,
        ImplType,
        Factory,
    }

    internal ActionType GetDescriptorActionType(IocDescriptor descriptor) =>
        descriptor switch
        {
            { Implementation: { } } => ActionType.CachedInstance,
            { Factory: { } } => ActionType.Factory,
            { ImplType: { } } => ActionType.ImplType,
            _ => ActionType.Invalid
        };

    private ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>? TryGetDictFor(IocLifetime lifetime) =>
        lifetime switch
        {
            IocLifetime.Singleton => Singletons,
            IocLifetime.Scoped => Scoped,
            _ => null
        };

    private object? TryGetImplFor(IocDescriptor descriptor) =>
        descriptor.Implementation ?? TryGetDictFor(descriptor)?.GetValueOrDefault(descriptor)?.Instance;

    private object? TryGetDisposingImplFor(IocDescriptor descriptor) =>
        IsRootContainerScope || descriptor.Lifetime is IocLifetime.Singleton
            ? TryGetImplFor(descriptor)
            : descriptor.Implementation is not null
                ? null
                : TryGetDictFor(descriptor)?.GetValueOrDefault(descriptor)?.Instance;
    
    //
    // scope mutability:
    
    // public bool IsReadOnly { get; private set; }
    //
    // public void MakeReadOnly() => IsReadOnly = true;
    //
    // private void ValidateMutability()
    // {
    //     if (IsReadOnly) throw new InvalidOperationException("Container is readonly.");
    // }
    //
    // private bool IsMutationForbidden(IocLifetime lifetime) =>
    //     IsReadOnly || (lifetime is IocLifetime.Scoped && IsDisposed) || RootContainerScope.IsDisposed;
    
    //

    #region Disposal
    
    // obj / descriptor's Implementation-property disposal:

    // private void WhenDisposedDisposeImplOf(IocDescriptor descriptor)
    // {
    //     if (!IsMutationForbidden(descriptor)) return;
    //     // if ((descriptor == IocLifetime.Scoped && IsDisposed) || RootContainerScope.IsDisposed) { }
    //     var impl = TryGetImplFor(descriptor);
    //     TryDisposalOf(impl);
    // }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WhenDisposedDisposeImplFrom(IocDescriptor descriptor)
    {
        if ((IsDisposed && descriptor == IocLifetime.Scoped) || RootContainerScope.IsDisposed)
            TryDisposalOf(descriptor.Implementation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TryDisposalOf(object? obj)
    {
        switch (obj)
        {
            case null: return;
            case IDisposable d:
                d.Dispose();
                return;
            case IAsyncDisposable ad:
                ad.DisposeAsync().GetAwaiter().GetResult();
                return;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask TryDisposalOfAsync(object? obj)
    {
        switch (obj)
        {
            case null: return default;
            case IAsyncDisposable ad:
                return ad.DisposeAsync();
            case IDisposable d:
                d.Dispose();
                return default;
            default: return default;
        }
    }
    
    // Scope Disposal:

    private async ValueTask AsyncDispose()
    {
        // async dispose:
        foreach (var descriptor in Descriptors)
        {
            if (!IsRootContainerScope && descriptor.Lifetime is not IocLifetime.Scoped) continue;
            // Singletons get disposed only when this is the root IoC Container:
            var impl = TryGetDisposingImplFor(descriptor);
            switch (impl)
            {
                case null:
                    continue;
                case IAsyncDisposable asyncDisposable:
                    await Task.Yield();
                    await asyncDisposable.DisposeAsync();
                    continue;
                case IDisposable disposable:
                    await Task.Yield();
                    disposable.Dispose();
                    continue;
            }
        }
    }
    private void SyncDispose()
    {
        // sync dispose:
        foreach (var descriptor in Descriptors)
        {
            if (!IsRootContainerScope && descriptor.Lifetime is not IocLifetime.Scoped) continue;
            // Singletons get disposed only when this is the root IoC Container:
            var impl = TryGetDisposingImplFor(descriptor);
            switch (impl)
            {
                case null:
                    continue;
                case IDisposable disposable:
                    disposable.Dispose();
                    continue;
                case IAsyncDisposable asyncDisposable:
                    asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
                    continue;
            }
        }
    }
    
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
        lock (Descriptors)
        {
            if (IsDisposed) return;
            IsDisposed = true;
            GC.SuppressFinalize(this);

            // sync dispose:
            Dispose(true);
        }
    }
    public ValueTask DisposeAsync()
    {
        if (IsDisposed) return default;
        lock (Descriptors)
        {
            if (IsDisposed) return default;
            IsDisposed = true;
            GC.SuppressFinalize(this);

            // sync common and or unmanaged dispose (without executing SyncDispose()):
            Dispose(false);
            LockScopedContainer();
            
            // async dispose:
            return AsyncDispose();
        }
    }

    private void LockScopedContainer()
    {
        // todo Lock scoped Container in the sense of - no more service resolvation possible, don't affect root provider
    }
    
    #endregion
}

