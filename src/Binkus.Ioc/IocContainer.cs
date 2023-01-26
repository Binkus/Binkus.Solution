using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using Binkus.DependencyInjection.Extensions;

// ReSharper disable UnusedAutoPropertyAccessor.Global
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


internal sealed record ServiceInstanceProvider(object? Instance = null)
{
    public object? Instance { get; internal set; } = Instance;
    
    // todo evaluate adding sth. like "bool IsDisposed" in regards to scopes,
    // see "object? TryGetDisposingImplFor(IocDescriptor descriptor)" in IocContainerScope
    // and think about potential dilemmas, e.g. double (potential) disposing -
    // what if double-disposal throws exception, if double-disposal is possible (evaluate that first),
    // saving here a property IsDisposed could rescue the day, or a try-catch.
}

// Scope Engine

public sealed record IocContainerScope : IServiceProvider, IContainerScope,
    IContainerScopeFactory, IAsyncDisposable, IDisposable, IEnumerable<IocDescriptor>
{
    public List<IocDescriptor>.Enumerator GetEnumerator()
    {
        Root.RwLock.EnterReadLock();
        try
        {
            return Descriptors.ToList().GetEnumerator();
        }
        finally
        {
            Root.RwLock.ExitReadLock();
        }
    }
    IEnumerator<IocDescriptor> IEnumerable<IocDescriptor>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // WIP
    public sealed record ContainerOptions
    {
        public static ContainerOptions Default { get; } = new(); 
        
        // e.g. Transient, if no descriptor registered, try create instance anyway
        public IocLifetime? HandleUnregisteredServicesAs { get; init; }
        // if false above can only be null or transient;
        public bool RegisterUnregisteredServicesOnHandleUnregistered { get; init; } = true;
        
        // if true handle RegisterUnregisteredServicesOnHandleUnregistered as false:
        public bool IsReadOnly { get; init; }
        public bool TriesCreatingScopeForWrappedProvider { get; init; } = true;

        public WrappedProviderExecutionChainPositionEnum WrappedProviderExecutionChainPosition { get; init; } =
            WrappedProviderExecutionChainPositionEnum.Last;

        public enum WrappedProviderExecutionChainPositionEnum
        {
            First,
            Last,
        }

        // public delegate object? GetServiceDelegate(Type serviceType);
        //
        // public GetServiceDelegate? GetServiceFun { get; init; }
        //
        // public object? GetService(IocContainerScope services, Type serviceType)
        // {
        //     return services.GetService(serviceType);
        // }
    }

    internal sealed record RootProperties
    {
        // private readonly IServiceProvider? _wrappedProvider;
        // internal IServiceProvider? WrappedProvider { get => _wrappedProvider; init => _wrappedProvider = Equals(value, RootContainerScope) ? _wrappedProvider : value; }
        
        internal required IocContainerScope RootContainerScope { get; init; }
        
        internal ReaderWriterLockSlim RwLock { get; } = new(LockRecursionPolicy.SupportsRecursion);
        internal required List<IocDescriptor> Descriptors { get; init; }
        internal required List<IocDescriptor> ScopedDescriptors { get; init; }
        
        internal required ConcurrentDictionary<Type, IocDescriptor> CachedDescriptors { get; init; }
    
        internal required ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> Singletons { get; init; }
    }
    
    internal RootProperties Root { get; }
    
    private readonly IServiceProvider? _wrappedProvider;
    public IServiceProvider? WrappedProvider { get => _wrappedProvider; init => _wrappedProvider = Equals(value, this) || (!ReferenceEquals(this, RootContainerScope) && Equals(value, RootContainerScope)) ? _wrappedProvider : value; }
    // public IServiceProvider? WrappedProvider { get => Root.WrappedProvider; init => _wrappedProvider = Equals(value, this) ? _wrappedProvider : value; }
    public IServiceProvider Services => this;
    public IocContainerScope RootContainerScope => Root.RootContainerScope;
    public IocContainerScope? ParentContainerScope => WeakParentContainerScope?.TryGetTarget(out var target) ?? false ? target : null;
    private WeakReference<IocContainerScope>? WeakParentContainerScope { get; }
    public ServiceScopeId Id { get; internal init; }
    public bool IsRootContainerScope { get; internal init; }

    private static IEnumerable<KeyValuePair<Type, IocDescriptor>> ToKeyValuePair(IEnumerable<IocDescriptor> services) 
        => services.Select(d => new KeyValuePair<Type, IocDescriptor>(d.ServiceType, d));

    public IocContainerScope() : this(default(IEnumerable<IocDescriptor>), default(ServiceScopeId)) { }
    internal IocContainerScope(ServiceScopeId? id) : this(default(IEnumerable<IocDescriptor>), id) { }
    public IocContainerScope(IEnumerable<IocDescriptor> services) : this(services, default(ServiceScopeId)) { }
    internal IocContainerScope(IEnumerable<IocDescriptor>? services, ServiceScopeId? id)
    {
        // WrappedProvider ...
        
        Id = id ?? new ServiceScopeId();
        IsRootContainerScope = true;
        
        var descriptors = services?.ToList() ?? new List<IocDescriptor>();
        
        Root = new RootProperties
        {
            RootContainerScope = this,
            Descriptors = descriptors,
            CachedDescriptors = new ConcurrentDictionary<Type, IocDescriptor>(),
            ScopedDescriptors = new List<IocDescriptor>(),
            // ScopedDescriptors = descriptors.Where(d => d.Lifetime is IocLifetime.Scoped).ToList(),
            Singletons = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>(),
        };
        Scoped = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>();
        
        InternalAddThisAsService();
        AddBasicServices();
        
        if (services == null) return;

        foreach (var descriptor in Descriptors)
        {
            descriptor.ThrowOnInvalidity();
            
            // Newer descriptors (farther in the list) replace old ones when ServiceType is equal
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
                static (_, d) => d, 
                static (_, _, d) => d,
                descriptor);
#else
            CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
                (_) => descriptor, 
                (_, _) => descriptor);
#endif

            switch (descriptor.Lifetime)
            {
                case IocLifetime.Singleton:
                    InternalAddServiceImpls(Singletons, descriptor);
                    continue;
                case IocLifetime.Scoped:
                    InternalAddServiceImpls(Scoped, descriptor);
                    ScopedDescriptors.Add(descriptor);
                    continue;
                case IocLifetime.Transient:
                default: continue;
            }
        }
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
        var d1 = IocDescriptor.CreateScoped(p => p);
        var d2 = IocDescriptor.CreateScoped<IocContainerScope>(p => (IocContainerScope)p);
        
        Scoped.TryAdd(d1, new ServiceInstanceProvider());
        Scoped.TryAdd(d2, new ServiceInstanceProvider());
        CachedDescriptors.TryAdd(d1.ServiceType, d1);
        CachedDescriptors.TryAdd(d2.ServiceType, d2);
        
        // not necessarily needed to add them here but for more consistency:
        // todo evaluate inserting at the beginning of the lists, for better extensibility:
        ScopedDescriptors.Add(d1);
        ScopedDescriptors.Add(d2);
        Descriptors.Add(d1);
        Descriptors.Add(d2);
    }

    // fork - WIP
    private IocContainerScope(bool fork, IocContainerScope current, ServiceScopeId? forkId = null)
    {
        Id = forkId ?? new ServiceScopeId();
        if (fork)
        {
            current.Root.RwLock.EnterReadLock();
            try
            {
                Root = current.Root with
                {
                    RootContainerScope = this,
                    Descriptors = current.Descriptors.ToList(),
                };
                
                Scoped = current.Scoped;
            }
            finally
            {
                current.Root.RwLock.ExitReadLock();
            }
        }
        else
        {
            current.Root.RwLock.EnterReadLock();
            try
            {
                Root = current.Root with
                {
                    RootContainerScope = this,
                    Descriptors = current.Descriptors.ToList(),
                };
                
                Scoped = current.Scoped;
            }
            finally
            {
                current.Root.RwLock.ExitReadLock();
            }
        }
    }
    
    // WIP
    internal IocContainerScope AsRootScope(bool sameId = true)
    {
        return new IocContainerScope(false, this, sameId ? Id : null);
    }
    
    // WIP
    internal IocContainerScope Fork(bool sameId)
    {
        return new IocContainerScope(true, this, sameId ? Id : null);
    }
    
    // Creates Scope
    private IocContainerScope(IocContainerScope parentContainerScope)
    {
        Root = parentContainerScope.Root;
        _wrappedProvider =
            parentContainerScope._wrappedProvider?.GetService<IContainerScopeFactory>()?.CreateScope().Services ??
            parentContainerScope._wrappedProvider; // create scope from wrapped provider
        WeakParentContainerScope = new WeakReference<IocContainerScope>(parentContainerScope);
        Id = new ServiceScopeId();
        IsRootContainerScope = false;
        Scoped = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>();

        foreach (var descriptor in ScopedDescriptors) 
            InternalAddServiceImpls(Scoped, descriptor);
    }

    internal IocContainerScope CreateScope(ServiceScopeId id) => new(this) { Id = id };
    
    IContainerScope IContainerScopeFactory.CreateScope() => CreateScope();
    public IocContainerScope CreateScope() => new(this);

    internal List<IocDescriptor> Descriptors => Root.Descriptors;
    internal List<IocDescriptor> ScopedDescriptors => Root.ScopedDescriptors;

    internal ConcurrentDictionary<Type, IocDescriptor> CachedDescriptors => Root.CachedDescriptors;

    internal ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> Singletons => Root.Singletons;
    
    internal ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> Scoped { get; }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        descriptor.ThrowOnInvalidity();
        if (!lockDescriptors)
            return InternalUnsafeAdd(descriptor);
        
        Root.RwLock.EnterWriteLock();
        try
        {
            return InternalUnsafeAdd(descriptor);
        }
        finally
        {
            Root.RwLock.ExitWriteLock();
        }
    }

    private bool InternalUnsafeAdd(IocDescriptor descriptor)
    {
        // if (IsReadOnly) return false;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
            static (_, d) => d, static (_, _, d) => d, descriptor);
#else
        CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
            (_) => descriptor, (_, _) => descriptor);
#endif
        Descriptors.Add(descriptor);
        if (descriptor.Lifetime is IocLifetime.Transient) return true;
        if (descriptor.Lifetime is IocLifetime.Scoped) ScopedDescriptors.Add(descriptor);
        var dict =
            descriptor.Lifetime is IocLifetime.Scoped ? Scoped : Singletons;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        dict.AddOrUpdate(descriptor, static (_, d) => new ServiceInstanceProvider(d.Implementation),
            static (_, s, d) =>
            {
                s.Instance = d.Implementation;
                return s;
            }, descriptor);
#else
        dict.AddOrUpdate(descriptor, (_) => new ServiceInstanceProvider(descriptor.Implementation),
            (_, s) =>
            {
                s.Instance = descriptor.Implementation;
                return s;
            });
#endif
        WhenDisposedDisposeImplFrom(descriptor);
        return true;
    }

    private bool TryAdd(IocDescriptor descriptor, bool lockDescriptors)
    {
        descriptor.ThrowOnInvalidity();
        if (CachedDescriptors.ContainsKey(descriptor.ServiceType)) return false;
        if (!lockDescriptors)
            return InternalUnsafeInnerLockTryAdd(descriptor);
        
        Root.RwLock.EnterWriteLock();
        try
        {
            return InternalUnsafeInnerLockTryAdd(descriptor);
        }
        finally
        {
            Root.RwLock.ExitWriteLock();
        }
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

    public object? GetService([Pure] Type serviceType) =>
        InternalGetService(serviceType) ?? WrappedProvider?.GetService(serviceType);

    internal object? InternalGetService([Pure] Type serviceType) =>
        InternalGetBasicService(serviceType) ?? GetSpecialService(serviceType);
    
    internal object? InternalGetBasicService([Pure] Type serviceType) =>
        CachedDescriptors.TryGetValue(serviceType, out var descriptor)
            ? GetServiceForDescriptor(descriptor)
            : null;

    // resolves special services like IEnumerable<T> or registered open generics
    internal object? GetSpecialService([Pure] Type serviceType)
    {
        /*
         * serviceType -> IEnumerable<T> -> IEnumerable<> has to be registered
         * registered implType for IEnumerable<> -> implType.MakeGenericType(serviceType.GetGenericParameters())
         */
        
        // bool isOpenGeneric = serviceType.IsGenericTypeDefinition;
        // bool isNotGeneric = !serviceType.IsGenericType;
        if (serviceType.IsGenericTypeDefinition || !serviceType.IsGenericType) return null;
        
        var openGenericType = serviceType.GetGenericTypeDefinition();
        
        // try resolve special service like IEnumerable<> or Lazy<> or Lazy<IEnumerable<>> or Lazy<IEnumerable<Lazy<>>>:
        // todo enable Special Service Resolve
        // object? specialServiceResult = GetSpecialService(serviceType, openGenericType);
        // if (specialServiceResult is not null) return specialServiceResult;
        
        return TryGetOpenGenericService(serviceType, openGenericType);
    }

    internal object? TryGetOpenGenericService([Pure] Type serviceType, [Pure] Type openGenericType)
    {
        bool isOpenGeneric = serviceType.IsGenericTypeDefinition;
        return null;
    }

    private void SpecialService(IocDescriptor descriptor)
    {
        
    }
    
    internal object? GetServiceForDescriptor(IocDescriptor descriptor) => 
        descriptor.Lifetime switch
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

    internal static object? TryCreateService(IServiceProvider provider, [Pure] IocDescriptor descriptor) =>
        descriptor.ServiceType.IsGenericTypeDefinition
            ? null
            : TryCreateService(provider, descriptor.ImplType ?? descriptor.ServiceType);

    internal static object? TryCreateService(IServiceProvider provider, [Pure] Type implType)
    {
        return implType.IsGenericTypeDefinition || implType.IsAbstract
            ? null
            : provider.GetRequiredService<IocUtilitiesDelegation>().CreateInstance(provider, implType);
        // return provider.TryCreateInstance(type);
        // return IocUtilitiesDelegation.Default.CreateInstance(provider, type);
    }
    
    internal enum ActionType
    {
        Invalid,
        CachedInstance,
        ImplType,
        Factory,
        OpenGenericFactory,
    }

    [Pure]
    internal static ActionType GetDescriptorActionType([Pure] IocDescriptor descriptor) =>
        descriptor switch
        {
            { Implementation: { } } => ActionType.CachedInstance,
            { Factory: { } } => ActionType.Factory,
            { ImplType: { } } => ActionType.ImplType,
            { OpenGenericFactory: { } } => ActionType.OpenGenericFactory,
            _ => ActionType.Invalid
        };

    [Pure] 
    private ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>? TryGetDictFor([Pure] IocLifetime lifetime) =>
        lifetime switch
        {
            IocLifetime.Singleton => Singletons,
            IocLifetime.Scoped => Scoped,
            _ => null
        };

    [Pure]
    private object? TryGetImplFor([Pure] IocDescriptor descriptor) =>
        descriptor.Implementation ?? TryGetDictFor(descriptor)?.GetValueOrDefault(descriptor)?.Instance;

    [Pure]
    private object? TryGetDisposingImplFor([Pure] IocDescriptor descriptor) =>
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
#pragma warning disable VSTHRD002
                // var vt = ad.DisposeAsync();
                // if(!vt.IsCompletedSuccessfully)
                //     vt.AsTask().GetAwaiter().GetResult();
                ad.DisposeAsync().AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
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

#pragma warning disable VSTHRD200
    private async ValueTask AsyncDispose(bool yield)
#pragma warning restore VSTHRD200
    {
        // async dispose:
        // Singletons get disposed only when this is the root IoC Container:
        if (!yield)
        {
            foreach (object? impl in from descriptor in Descriptors
                     where IsRootContainerScope || descriptor.Lifetime is IocLifetime.Scoped
                     select TryGetDisposingImplFor(descriptor))
            {
                await TryDisposalOfAsync(impl).ConfigureAwait(false);
            }
            return;
        }

        foreach (object? impl in from descriptor in Descriptors
                 where IsRootContainerScope || descriptor.Lifetime is IocLifetime.Scoped
                 select TryGetDisposingImplFor(descriptor))
        {
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
#pragma warning disable VSTHRD002
                    // asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
                    // var vt = asyncDisposable.DisposeAsync();
                    // if(!vt.IsCompletedSuccessfully)
                    //     vt.AsTask().GetAwaiter().GetResult();
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
                    continue;
            }
        }
    }
    
    public bool IsDisposed { get; private set; }
    // ~IocContainerScope() => Dispose(false);
    private void Dispose(bool disposing)
    {
        // ReleaseUnmanagedResourcesIfSomeGetAdded:
        // here, and re-add commented-out finalizer as well as GC.SuppressFinalize(this)-calls 
        
        if (!disposing) return;
        LockScopedContainer();
        
        // sync dispose:
        SyncDispose();
    }
    
    public void Dispose()
    {
        if (IsDisposed) return;
        Root.RwLock.EnterReadLock();
        try
        {
            // todo evaluate re-adding an additional!! exclusive lock to prevent multiple simultaneous Dispose calls, or re-use WriteLock instead 
            if (IsDisposed) return;
            IsDisposed = true;
            // GC.SuppressFinalize(this);

            // sync dispose:
            Dispose(true);
        }
        finally
        {
            Root.RwLock.ExitReadLock();
        }
    }
    public ValueTask DisposeAsync() => DisposeAsync(true);
    public async ValueTask DisposeAsync(bool yield)
    {
        // has to be async cause of try-finally block for ReadLock
        if (IsDisposed) return;
        Root.RwLock.EnterReadLock();
        try
        {
            // todo evaluate re-adding an additional!! exclusive lock to prevent multiple simultaneous Dispose calls, or re-use WriteLock instead
            if (IsDisposed) return;
            IsDisposed = true;
            // GC.SuppressFinalize(this);

            // sync common and or unmanaged dispose (without executing SyncDispose()):
            Dispose(false);
            LockScopedContainer();
            
            // async dispose:
            await AsyncDispose(yield).ConfigureAwait(false);
        }
        finally
        {
            Root.RwLock.ExitReadLock();
        }
    }

    private void LockScopedContainer()
    {
        // todo evaluate following todo:
        // todo evaluate locking scoped Container in the sense of - no more service resolvation possible, don't affect root provider
    }
    
    #endregion
}

