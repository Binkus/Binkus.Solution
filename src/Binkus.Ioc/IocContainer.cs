using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    
    internal IocContainerScope RootContainerScope { get; set; }
    internal IocContainerScope ContainerScope { get; set; }
}

public static class IocContainerBuilder
{
    public static IContainerScope BuildIocContainer() => new IocContainerScope();
    
    public static IContainerScope BuildIocContainer(this IEnumerable<IocDescriptor> services)
    {
        // Action<IocContainerScope.ContainerOptions>? configure = null;
        // configure?.Invoke(new IocContainerScope.ContainerOptions());
        return new IocContainerScope(services);
    }
}

public interface IContainerScopeFactory
{
    IContainerScope CreateScope();
}

public interface IContainerScope : IContainerScopeFactory, IAsyncDisposable, IDisposable
{
    public IServiceProvider Services { get; }
}

// public interface IContainerScopeProvider : IServiceProvider, IContainerScope { }


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

internal sealed record IocContainerScope : IServiceProvider, IContainerScope,
    IContainerScopeFactory, IAsyncDisposable, IDisposable, IEnumerable<IocDescriptor>, ICollection<IocDescriptor>
{
    [Pure]
    public List<IocDescriptor>.Enumerator GetEnumerator()
    {
        Root.RwLock.EnterReadLock();
        try
        {
            return Root.Descriptors.ToList().GetEnumerator();
        }
        finally
        {
            Root.RwLock.ExitReadLock();
        }
    }
    [Pure] IEnumerator<IocDescriptor> IEnumerable<IocDescriptor>.GetEnumerator() => GetEnumerator();
    [Pure] IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    // ICollection<IocDescriptor>

    void ICollection<IocDescriptor>.Add(IocDescriptor item) => Add(item);
    
    void ICollection<IocDescriptor>.Clear()
    {
        if (Root.Container.Options.IsReadOnly || Options.IsReadOnly) throw new NotSupportedException();

        Root.RwLock.EnterWriteLock();
        try
        {
            Dispose();
            Root.Container.Dispose();
            
            Root.Descriptors.Clear();
            Root.Singletons.Clear();
            Root.ScopedDescriptors.Clear();
            
            Root.Container.InternalAddThisAsService();
            Root.Container.AddBasicServices();
        }
        finally
        {
            Root.RwLock.ExitWriteLock();
        }
    }

    bool ICollection<IocDescriptor>.Contains(IocDescriptor item)
    {
        if (Root.CachedDescriptors.Values.Contains(item))
            return true;
        Root.RwLock.EnterReadLock();
        try
        {
            return Root.Descriptors.Contains(item);
        }
        finally
        {
            Root.RwLock.ExitReadLock();
        }
    }

    void ICollection<IocDescriptor>.CopyTo(IocDescriptor[] array, int arrayIndex)
    {
        var arr = this.ToArray();
        Array.Copy(arr, 0, array, arrayIndex, arr.Length);
        // var a = this.Skip(arrayIndex).ToArray();
        // for (var i = 0; i < a.Length; i++) array[i] = a[i];
    }

    bool ICollection<IocDescriptor>.Remove(IocDescriptor item)
    {
        if (Root.Container.Options.IsReadOnly || Options.IsReadOnly) throw new NotSupportedException();

        throw new NotImplementedException();
    }

    public int Count => Root.Descriptors.Count;

    bool ICollection<IocDescriptor>.IsReadOnly => Root.Container.Options.IsReadOnly || Options.IsReadOnly;
    
    //

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
        public bool StoreWeakReferenceToParent { get; init; }
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
        
        public bool YieldBeforeEachDispose { get; init; }

        // public bool RespectSynchronizationContextOnDisposal { get => ContinueOnCapturedContextWhenDisposeAsync; init => ContinueOnCapturedContextWhenDisposeAsync = value; }
        public bool ContinueOnCapturedContextWhenDisposeAsync { get; init; } = false;

        private Func<IAsyncDisposable, ConfiguredValueTaskAwaitable>? _disposeAsyncFunc;
        internal Func<IAsyncDisposable, ConfiguredValueTaskAwaitable> DisposeAsyncFunc
        {
            get => _disposeAsyncFunc ??= ContinueOnCapturedContextWhenDisposeAsync
                ? DisposeAsyncWithCapturedContext
                : DisposeAsyncWithoutCapturedContext;
            init => _disposeAsyncFunc = value;
        }

        private static readonly Func<IAsyncDisposable, ConfiguredValueTaskAwaitable>
            DisposeAsyncWithCapturedContext = static ad => ad.DisposeAsync().ConfigureAwait(true);

        private static readonly Func<IAsyncDisposable, ConfiguredValueTaskAwaitable>
            DisposeAsyncWithoutCapturedContext = static ad => ad.DisposeAsync().ConfigureAwait(false);
        
        
        private Action<IAsyncDisposable>? _syncDisposeAsyncFunc;
        internal Action<IAsyncDisposable> SyncDisposeAsyncFunc
        {
            get => _syncDisposeAsyncFunc ??= ContinueOnCapturedContextWhenDisposeAsync
                ? SyncDisposeAsyncWithCapturedContext
                : SyncDisposeAsyncWithoutCapturedContext;
            init => _syncDisposeAsyncFunc = value;
        }

#pragma warning disable VSTHRD002
        private static readonly Action<IAsyncDisposable>
            SyncDisposeAsyncWithCapturedContext = static ad =>
                ad.DisposeAsync().AsTask().GetAwaiter().GetResult();

        private static readonly Action<IAsyncDisposable>
            SyncDisposeAsyncWithoutCapturedContext = static ad =>
                Task.Run(() => ad.DisposeAsync().AsTask()).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    internal sealed record RootProperties
    {
        // private readonly IServiceProvider? _wrappedProvider;
        // internal IServiceProvider? WrappedProvider { get => _wrappedProvider; init => _wrappedProvider = Equals(value, RootContainerScope) ? _wrappedProvider : value; }
        
        internal required IocContainerScope Container { get; init; }

        internal readonly object Gate = new();
        internal ReaderWriterLockSlim RwLock { get; } = new(LockRecursionPolicy.SupportsRecursion);
        internal List<IocDescriptor> InternalDescriptors { get; init; } = new(6);
        internal required List<IocDescriptor> Descriptors { get; init; }
        internal required List<IocDescriptor> ScopedDescriptors { get; init; }
        
        internal required ConcurrentDictionary<Type, IocDescriptor> CachedDescriptors { get; init; }
    
        internal required ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider> Singletons { get; init; }
    }
    
    internal RootProperties Root { get; }
    internal ContainerOptions Options { get; }
    
    private IServiceProvider[]? _firstWrappedProviders;
    private IServiceProvider[]? _lastWrappedProviders;

    internal IServiceProvider[] FirstWrappedProviders
    {
        set
        {
            Monitor.Enter(Root.Gate);
            _firstWrappedProviders = value;
            var len = value.Length;
            if (len == 0) GetServiceFunc = static (scope, type) => scope.InternalGetServiceWithLastWrappers(type);
            else if (len == 1)
                GetServiceFunc = static (scope, type) => scope._firstWrappedProviders![0].GetService(type) ??
                                                         scope.InternalGetServiceWithLastWrappers(type);
            else if (len == 2)
                GetServiceFunc = static (scope, type) => scope._firstWrappedProviders![0].GetService(type) ??
                                                         scope._firstWrappedProviders[1].GetService(type) ??
                                                         scope.InternalGetServiceWithLastWrappers(type);
            else if (len == 3)
                GetServiceFunc = static (scope, type) => scope._firstWrappedProviders![0].GetService(type) ??
                                                         scope._firstWrappedProviders[1].GetService(type) ??
                                                         scope._firstWrappedProviders[2].GetService(type) ??
                                                         scope.InternalGetServiceWithLastWrappers(type);
            else if (len == 4)
                GetServiceFunc = static (scope, type) => scope._firstWrappedProviders![0].GetService(type) ??
                                                         scope._firstWrappedProviders[1].GetService(type) ??
                                                         scope._firstWrappedProviders[2].GetService(type) ??
                                                         scope._firstWrappedProviders[3].GetService(type) ??
                                                         scope.InternalGetServiceWithLastWrappers(type);
            else GetServiceFunc = static (scope, type) => scope.InternalGetServiceWithWrapperArrays(type);
            Monitor.Exit(Root.Gate);
        }
    }

    internal IServiceProvider[] LastWrappedProviders { set => _lastWrappedProviders = value; }
    // private readonly IServiceProvider? _wrappedProvider;
    // public IServiceProvider? WrappedProvider { get => _wrappedProvider; init => _wrappedProvider = Equals(value, this) || (!ReferenceEquals(this, RootContainerScope) && Equals(value, RootContainerScope)) ? _wrappedProvider : value; }
    // public IServiceProvider? WrappedProvider { get => Root.WrappedProvider; init => _wrappedProvider = Equals(value, this) ? _wrappedProvider : value; }
    public IServiceProvider Services => this;
    // public IocContainerScope RootContainerScope => Root.Container;
    [NotNullIfNotNull(nameof(HardParentContainerScope))]
    public IocContainerScope? ParentContainer => HardParentContainerScope ??
                                                 (WeakParentContainerScope?
                                                     .TryGetTarget(out var target) ?? false
                                                     ? target
                                                     : null);
    private WeakReference<IocContainerScope>? WeakParentContainerScope { get; }
    private IocContainerScope? HardParentContainerScope { get; }
    public ServiceScopeId Id { get; internal init; }
    public bool IsRootContainerScope { get; internal init; }

    private static IEnumerable<KeyValuePair<Type, IocDescriptor>> ToKeyValuePair(IEnumerable<IocDescriptor> services) 
        => services.Select(d => new KeyValuePair<Type, IocDescriptor>(d.ServiceType, d));

    public IocContainerScope() : this(default(IEnumerable<IocDescriptor>), default(ServiceScopeId)) { }
    internal IocContainerScope(ServiceScopeId? id) : this(default(IEnumerable<IocDescriptor>), id) { }
    public IocContainerScope(IEnumerable<IocDescriptor> services) : this(services, default(ServiceScopeId)) { }
    internal IocContainerScope(IEnumerable<IocDescriptor>? services, ServiceScopeId? id, ContainerOptions? options = null)
    {
        Options = options ?? ContainerOptions.Default;
        
        // WrappedProvider ...
        
        Id = id ?? new ServiceScopeId();
        IsRootContainerScope = true;
        
        const int defaultCapacity = 31;
        var descriptors = services?.ToList() ?? new List<IocDescriptor>();
        var capacity = descriptors.Count;
        capacity = capacity < defaultCapacity ? defaultCapacity : capacity;
        var specificCapacity = capacity / 2 < defaultCapacity ? defaultCapacity : capacity / 2;
        
        Root = new RootProperties
        {
            Container = this,
            Descriptors = descriptors,
            CachedDescriptors = new ConcurrentDictionary<Type, IocDescriptor>(1, capacity),
            ScopedDescriptors = new List<IocDescriptor>(),
            // ScopedDescriptors = descriptors.Where(d => d.Lifetime is IocLifetime.Scoped).ToList(),
            Singletons = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>(1, specificCapacity),
        };
        Scoped = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>(1, specificCapacity);
        
        InternalAddThisAsService();
        AddBasicServices();
        
        if (services == null) return;

        foreach (var descriptor in Root.Descriptors)
        {
            descriptor.ThrowOnInvalidity();
            
            // Newer descriptors (farther in the list) replace old ones when ServiceType is equal
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            Root.CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
                static (_, d) => d, 
                static (_, _, d) => d,
                descriptor);
#else
            Root.CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
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
                    Root.ScopedDescriptors.Add(descriptor);
                    continue;
                case IocLifetime.Transient:
                default: continue;
            }
        }
    }

    private void AddBasicServices()
    {
        var type = typeof(IocUtilitiesDelegation);
        if (Root.CachedDescriptors.ContainsKey(type)) return;
        var d = IocDescriptor.CreateSingleton(IocUtilitiesDelegation.NewUninitializedIocUtilitiesDelegation());
        if (!Root.CachedDescriptors.TryAdd(type, d)) return;
        Root.Descriptors.Add(d);
        Singletons.TryAdd(d, new ServiceInstanceProvider(d.Implementation));
        
        InternalUnsafeInnerLockTryAdd(IocDescriptor.CreateTransient<IEnumerable<IocDescriptor>>(_ => this.ToList()));
    }
    
    private void InternalAddThisAsService()
    {
        var d1 = IocDescriptor.CreateScoped(p => p);
        var d2 = IocDescriptor.CreateScoped<IocContainerScope>(p => (IocContainerScope)p);
        
        Scoped.TryAdd(d1, new ServiceInstanceProvider());
        Scoped.TryAdd(d2, new ServiceInstanceProvider());
        Root.CachedDescriptors.TryAdd(d1.ServiceType, d1);
        Root.CachedDescriptors.TryAdd(d2.ServiceType, d2);
        
        // not necessarily needed to add them here but for more consistency:
        // todo evaluate inserting at the beginning of the lists, for better extensibility:
        Root.ScopedDescriptors.Add(d1);
        Root.ScopedDescriptors.Add(d2);
        Root.Descriptors.Add(d1);
        Root.Descriptors.Add(d2);
    }

    // fork - WIP
    private IocContainerScope(bool fork, IocContainerScope current, ContainerOptions? options = null, ServiceScopeId? forkId = null)
    {
        Options = options ?? current.Options;
        Id = forkId ?? new ServiceScopeId();
        if (fork)
        {
            current.Root.RwLock.EnterReadLock();
            try
            {
                Root = current.Root with
                {
                    Container = this,
                    Descriptors = current.Root.Descriptors.ToList(),
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
                    Container = this,
                    Descriptors = current.Root.Descriptors.ToList(),
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
    internal IocContainerScope AsRootScope(bool sameId = true, ContainerOptions? options = null)
    {
        return new IocContainerScope(false, this, options, sameId ? Id : null);
    }
    
    // WIP
    internal IocContainerScope Fork(bool sameId, ContainerOptions? options = null)
    {
        return new IocContainerScope(true, this, options, sameId ? Id : null);
    }

#nullable disable
    // Record's copy-ctor
    // ReSharper disable NotNullOrRequiredMemberIsNotInitialized
    private IocContainerScope(IocContainerScope parentContainerScope) => throw new NotSupportedException();
    // ReSharper restore NotNullOrRequiredMemberIsNotInitialized
#nullable restore
    
    // Creates Scope
    private IocContainerScope(IocContainerScope parentContainerScope, ContainerOptions? options)
    {
        Options = options ?? parentContainerScope.Options;
        Root = parentContainerScope.Root;
        GetServiceFunc = parentContainerScope.GetServiceFunc;
        // _wrappedProvider =
        //     parentContainerScope._wrappedProvider?.GetService<IContainerScopeFactory>()?.CreateScope().Services ??
        //     parentContainerScope._wrappedProvider; // create scope from wrapped provider
        SetWrapped(ref _firstWrappedProviders, parentContainerScope._firstWrappedProviders, this);
        SetWrapped(ref _lastWrappedProviders, parentContainerScope._lastWrappedProviders, this);
        static void SetWrapped(ref IServiceProvider[]? providersRef, IServiceProvider[]? parentProviders, IocContainerScope @this)
        {
            int len = parentProviders?.Length ?? 0;
            if (len is 0) return;
            providersRef = CreateScopedWrappedProviders();
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IServiceProvider[] CreateScopedWrappedProviders()
            {
                var result = new IServiceProvider[len];
                Array.Copy(parentProviders!, result, len);
                for (int i = 0; i < len; i++)
                {
                    // create scope from wrapped provider when IContainerScopeFactory is registered 
                    ref var r = ref result[i];
                    IContainerScope? s = r.GetService<IContainerScopeFactory>()?.CreateScope();
                    r = s?.Services ?? r;
                    if (r is IDisposable or IAsyncDisposable || s is null) continue;
                    @this.DisposeCancellationTokenSource ??= new CancellationTokenSource();
                    @this.DisposeCancellationTokenSource.Token.Register(static state =>
                            (((IContainerScope, Action<IAsyncDisposable> action))state!)
                            .action((((IContainerScope scope, Action<IAsyncDisposable>))state).scope),
                        (s, @this.Options.SyncDisposeAsyncFunc), @this.Options.ContinueOnCapturedContextWhenDisposeAsync);
                }
                return result;
            }
        }
        if (Options.StoreWeakReferenceToParent) // ~200ns cost when enabled, WeakReferences super slow to create. Hard reference would be usually bad.
            WeakParentContainerScope = new WeakReference<IocContainerScope>(parentContainerScope);
        Id = new ServiceScopeId();
        IsRootContainerScope = false;
        Scoped = new ConcurrentDictionary<IocDescriptor, ServiceInstanceProvider>(1, Root.ScopedDescriptors.Count);

        foreach (var descriptor in Root.ScopedDescriptors) 
            InternalAddServiceImpls(Scoped, descriptor);
    }

    internal IocContainerScope CreateScope(ServiceScopeId id) => new(this, null) { Id = id };
    
    IContainerScope IContainerScopeFactory.CreateScope() => CreateScope();
    public IocContainerScope CreateScope(ContainerOptions? options = null) => new(this, options);

    // internal List<IocDescriptor> Descriptors => Root.Descriptors;
    // internal List<IocDescriptor> ScopedDescriptors => Root.ScopedDescriptors;

    // internal ConcurrentDictionary<Type, IocDescriptor> CachedDescriptors => Root.CachedDescriptors;

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
        Root.CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
            static (_, d) => d, static (_, _, d) => d, descriptor);
#else
        Root.CachedDescriptors.AddOrUpdate(descriptor.ServiceType, 
            (_) => descriptor, (_, _) => descriptor);
#endif
        Root.Descriptors.Add(descriptor);
        if (descriptor.Lifetime is IocLifetime.Transient) return true;
        if (descriptor.Lifetime is IocLifetime.Scoped) Root.ScopedDescriptors.Add(descriptor);
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
        if (Root.CachedDescriptors.ContainsKey(descriptor.ServiceType)) return false;
        
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
        if (!Root.CachedDescriptors.TryAdd(descriptor.ServiceType, descriptor)) return false;
        Root.Descriptors.Add(descriptor);
        if (descriptor.Lifetime is IocLifetime.Transient) return true;
        if (descriptor.Lifetime is IocLifetime.Scoped) Root.ScopedDescriptors.Add(descriptor);
        var dict = 
            descriptor.Lifetime is IocLifetime.Scoped ? Scoped : Singletons;
        dict.TryAdd(descriptor, new ServiceInstanceProvider(descriptor.Implementation));
        WhenDisposedDisposeImplFrom(descriptor);
        return true;
    }

    public object? GetService(Type serviceType) => GetServiceFunc(this, serviceType);
    
    internal Func<IocContainerScope, Type, object?> GetServiceFunc { get; set; } =
        static (scope, type) => scope.InternalGetServiceWithLastWrappers(type);

    internal object? InternalGetServiceWithWrapperArrays(Type serviceType)
    {
        object? result;
        var len = _firstWrappedProviders?.Length ?? 0;
        for (int i = 0; i < len; i++)
            if ((result = _firstWrappedProviders![i].GetService(serviceType)) is not null)
                return result;
        if ((result = InternalGetService(serviceType)) is not null) return result;
        len = _lastWrappedProviders?.Length ?? 0;
        for (int i = 0; i < len; i++)
            if ((result = _lastWrappedProviders![i].GetService(serviceType)) is not null)
                return result;
        return null;
    }
    
    internal object? InternalLastWrapperArrayGetService(Type serviceType)
    {
        object? result;
        var len = _lastWrappedProviders?.Length ?? 0;
        for (int i = 0; i < len; i++)
            if ((result = _lastWrappedProviders![i].GetService(serviceType)) is not null)
                return result;
        return null;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? InternalGetServiceWithLastWrappers(Type serviceType) =>
        InternalGetService(serviceType) ?? InternalLastWrapperArrayGetService(serviceType);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? InternalGetService([Pure] Type serviceType) =>
        InternalGetBasicService(serviceType) ?? GetSpecialService(serviceType);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? InternalGetBasicService([Pure] Type serviceType) =>
        Root.CachedDescriptors.TryGetValue(serviceType, out var descriptor)
            ? GetServiceForDescriptor(descriptor)
            : null;

    // resolves special services like IEnumerable<T> or registered open generics
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object? GetSpecialService(Type serviceType)
    {
        if (serviceType.IsGenericTypeDefinition || !serviceType.IsGenericType) return null;
        
        var openGenericType = serviceType.GetGenericTypeDefinition();
        
        // try resolve special service like IEnumerable<> or Lazy<> or Lazy<IEnumerable<>> or Lazy<IEnumerable<Lazy<>>>:
        // todo enable Special Service Resolve
        // object? specialServiceResult = GetSpecialService(serviceType, openGenericType);
        // if (specialServiceResult is not null) return specialServiceResult;
        
        return TryGetOpenGenericService(serviceType, openGenericType);
    }

    internal object? TryGetOpenGenericService(Type serviceType, Type openGenericType)
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
            IocLifetime.Scoped => // IsDisposed && !IsRootContainerScope 
                // ? throw new InvalidOperationException("Scope is disposed.") :
                GetSingletonOrScopedService(Scoped, descriptor),
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
                return instanceProvider.Instance ??= WhenDisposedDispose(
                    descriptor.Factory?.Invoke(this) ?? TryCreateService(this, descriptor),
                    descriptor);
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
    private bool ShouldServiceBeDisposed(IocLifetime lifetime) =>
        Root.Container.IsDisposed || (IsDisposed && lifetime != IocLifetime.Singleton);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object? WhenDisposedDispose(object? impl, IocLifetime lifetime)
    {
        if (ShouldServiceBeDisposed(lifetime))
            TryDisposalOf(impl);
        return impl;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WhenDisposedDisposeImplFrom(IocDescriptor descriptor)
    {
        if (ShouldServiceBeDisposed(descriptor))
            TryDisposalOf(descriptor.Implementation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryDisposalOf(object? obj)
    {
        switch (obj)
        {
            case null: return;
            case IDisposable d:
                d.Dispose();
                return;
            case IAsyncDisposable ad:
                // in case the service only implements IAsyncDisposable and not IDisposable:
                Options.SyncDisposeAsyncFunc(ad);
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
    private async ValueTask AsyncDispose(bool yieldBeforeEachDispose)
#pragma warning restore VSTHRD200
    {
        // async dispose:
        // Singletons get disposed only when this is the root IoC Container:
        if (!yieldBeforeEachDispose)
        {
            Func<object?, ConfiguredValueTaskAwaitable> disposeObjectAsync =
                Options.ContinueOnCapturedContextWhenDisposeAsync
                    ? static o => TryDisposalOfAsync(o).ConfigureAwait(true)
                    : static o => TryDisposalOfAsync(o).ConfigureAwait(false);
            
            foreach (object? impl in from descriptor in Root.Descriptors
                     where IsRootContainerScope || descriptor.Lifetime is IocLifetime.Scoped
                     select TryGetDisposingImplFor(descriptor))
            {
                await disposeObjectAsync(impl);
            }
            return;
        }

        var disposeAsync = Options.DisposeAsyncFunc;
        foreach (object? impl in from descriptor in Root.Descriptors
                 where IsRootContainerScope || descriptor.Lifetime is IocLifetime.Scoped
                 select TryGetDisposingImplFor(descriptor))
        {
            switch (impl)
            {
                case null:
                    continue;
                case IAsyncDisposable asyncDisposable:
                    await Task.Yield();
                    await disposeAsync(asyncDisposable);
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
#if NET5_0_OR_GREATER // Root.Descriptors is read-locked anyway. For few ns performance gain:
        var descriptorsAsSpan = CollectionsMarshal.AsSpan(Root.Descriptors);
        ref var firstDescriptorManagedSmartPointer = ref MemoryMarshal.GetReference(descriptorsAsSpan);
        var len = descriptorsAsSpan.Length;
        for (var i = 0; i < len; i++)
        {
            var descriptor = Unsafe.Add(ref firstDescriptorManagedSmartPointer, i);
#else
        foreach (var descriptor in Root.Descriptors)
        {
#endif
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
                    // in case the service only implements IAsyncDisposable and not IDisposable
                    Options.SyncDisposeAsyncFunc(asyncDisposable);
                    continue;
            }
        }
    }
    
    internal CancellationTokenSource? DisposeCancellationTokenSource { get; private set; }
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
        Root.RwLock.EnterUpgradeableReadLock();
        Root.RwLock.EnterWriteLock();
        try
        {
            if (IsDisposed) return;
            IsDisposed = true;
            // GC.SuppressFinalize(this);
            Root.RwLock.ExitWriteLock();

            // sync dispose:
            // wrapped:
            var len = _firstWrappedProviders?.Length ?? 0;
            for (int i = 0; i < len; i++) DisposeWrapped(in _firstWrappedProviders![i]);
            len = _lastWrappedProviders?.Length ?? 0;
            for (int i = 0; i < len; i++) DisposeWrapped(in _lastWrappedProviders![i]);
            DisposeCancellationTokenSource?.Cancel();
            // this internal async dispose:
            Dispose(true);
        }
        finally
        {
            Root.RwLock.ExitUpgradeableReadLock();
        }
    }
    public ValueTask DisposeAsync() => DisposeAsync(Options.YieldBeforeEachDispose);
    public async ValueTask DisposeAsync(bool yieldBeforeEachDispose)
    {
        // has to be async cause of try-finally block for ReadLock
        if (IsDisposed) return;
        Root.RwLock.EnterUpgradeableReadLock();
        Root.RwLock.EnterWriteLock();
        try
        {
            if (IsDisposed) return;
            IsDisposed = true;
            // GC.SuppressFinalize(this);
            Root.RwLock.ExitWriteLock();

            // sync common and or unmanaged dispose (without executing SyncDispose()):
            Dispose(false);
            LockScopedContainer();

            // async dispose:
            // wrapped:
            var len = _firstWrappedProviders?.Length ?? 0;
            for (int i = 0; i < len; i++)
            {
                if(yieldBeforeEachDispose) await Task.Yield();
                await DisposeWrappedAsync(in _firstWrappedProviders![i]);
            }
            len = _lastWrappedProviders?.Length ?? 0;
            for (int i = 0; i < len; i++)
            {
                if(yieldBeforeEachDispose) await Task.Yield();
                await DisposeWrappedAsync(in _lastWrappedProviders![i]);
            }
            DisposeCancellationTokenSource?.Cancel();
            // this internal async dispose:
            await AsyncDispose(yieldBeforeEachDispose).ConfigureAwait(false);
        }
        finally
        {
            Root.RwLock.ExitUpgradeableReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ConfiguredValueTaskAwaitable DisposeWrappedAsync(scoped in IServiceProvider wrapped)
    {
        if (!IsRootContainerScope && wrapped.GetService<IContainerScopeFactory>() is null) return default;
        if (wrapped is IAsyncDisposable a)
            return Options.DisposeAsyncFunc(a);
        (wrapped as IDisposable)?.Dispose();
        return default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DisposeWrapped(scoped in IServiceProvider wrapped)
    {
        if (!IsRootContainerScope && wrapped.GetService<IContainerScopeFactory>() is null) return;
        if (wrapped is IDisposable d)
            d.Dispose();
        else if (wrapped is IAsyncDisposable a)
            Options.SyncDisposeAsyncFunc(a);
    }

    private void LockScopedContainer()
    {
        // todo evaluate following todo:
        // todo evaluate locking scoped Container in the sense of - no more service resolvation possible, don't affect root provider
    }
    
    #endregion
}

