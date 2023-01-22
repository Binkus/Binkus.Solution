using System.Diagnostics.CodeAnalysis;
#if !NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

// ReSharper disable MemberCanBePrivate.Global

namespace Binkus.DependencyInjection;

public sealed class IocDescriptor : IEquatable<IocDescriptor>
{
    [SetsRequiredMembers]
    public IocDescriptor(
        IocLifetime lifetime,
        Type serviceType, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implType)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implType);
#else
        ThrowIfNull(serviceType);
        ThrowIfNull(implType);
#endif
        _lifetime = ThrowOnInvalidLifetime(lifetime);
        ServiceType = serviceType;
        ImplType = implType;
        
        ThrowIfImplTypeIsNotAssignableToServiceType();
        ThrowIfImplTypeIsNotConstructable();
    }
    
    [SetsRequiredMembers]
    public IocDescriptor(
        IocLifetime lifetime, 
        Type serviceType, 
        Func<IServiceProvider, Type[], object> openGenericFactory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(openGenericFactory);
#else
        ThrowIfNull(serviceType);
        ThrowIfNull(openGenericFactory);
#endif
        _lifetime = ThrowOnInvalidLifetime(lifetime);
        ServiceType = serviceType;
        OpenGenericFactory = openGenericFactory;
    }
    
    [SetsRequiredMembers]
    public IocDescriptor(IocLifetime lifetime, Type serviceType, Func<IServiceProvider, object> factory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(factory);
#else
        ThrowIfNull(serviceType);
        ThrowIfNull(factory);
#endif
        _lifetime = ThrowOnInvalidLifetime(lifetime);
        ServiceType = serviceType;
        Factory = factory;
    }
    
    [SetsRequiredMembers]
    public IocDescriptor(Type serviceType, object implementation) : this(serviceType, implementation, false) { }
    
    [SetsRequiredMembers]
    internal IocDescriptor(Type serviceType, object implementation, bool scoped)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementation);
#else
        ThrowIfNull(serviceType);
        ThrowIfNull(implementation);
#endif
        _lifetime = scoped ? IocLifetime.Scoped : IocLifetime.Singleton;
        ServiceType = serviceType;
        ImplType = implementation.GetType();
        Implementation = implementation;
        
        ThrowIfImplTypeIsNotAssignableToServiceType();
    }
    
    [SetsRequiredMembers]
    internal IocDescriptor(IocDescriptor descriptor)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.ServiceType);
#else
        ThrowIfNull(descriptor);
        ThrowIfNull(descriptor.ServiceType);
#endif
        _lifetime = ThrowOnInvalidLifetime(descriptor.Lifetime);
        ServiceType = descriptor.ServiceType;
        ImplType = descriptor.ImplType;
        Factory = descriptor.Factory;
        Implementation = descriptor.Implementation;
        
        if (ImplType is null && Factory is null && Implementation is null)
            throw new InvalidOperationException($"Invalid {nameof(IocDescriptor)}");

        ThrowIfImplTypeIsNotAssignableToServiceType();
    }
    
    internal IocDescriptor Copy() => new(this);

    internal IocDescriptor() { }
    
    internal IocDescriptor With(/* todo */) // potential public API for creating new Descriptors with prev one as base 
    {
        return new IocDescriptor(this);
    }

#if !NET6_0_OR_GREATER
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    internal static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null) throw new ArgumentNullException(paramName);
    }
#endif

    internal void ThrowIfImplTypeIsNotAssignableToServiceType()
    {
#if NET5_0_OR_GREATER
        if (!ImplType?.IsAssignableTo(ServiceType) ?? false)
#else
        if (ImplType is not null && !ServiceType.IsAssignableFrom(ImplType))
#endif
            throw new InvalidOperationException(
                $"{Implementation}'s Type {ImplType} can't be assigned to {ServiceType}");
    }

    internal static Type? ImplTypeWhenConstructable(Type? implType) =>
        IsImplTypeNotConstructable(implType) ? null : implType;
    
    internal static bool IsImplTypeNotConstructable(Type? implType) =>
        implType is not null && (implType.IsAbstract || implType.IsGenericParameter);
    
    internal void ThrowIfImplTypeIsNotConstructable()
    {
        if (IsImplTypeNotConstructable(ImplType))
            throw new InvalidOperationException($"{nameof(ImplType)}:'{ImplType}' for " +
                                                $"{nameof(ServiceType)}:'{ServiceType}' is not constructable.");
        
        // if ((!ImplType?.IsGenericTypeDefinition ?? true) || (ImplType?.IsAbstract ?? true))
        //     throw new InvalidOperationException($"{nameof(ImplType)}:'{ImplType}' is not constructable.");
    }

    internal IocDescriptor ThrowOnInvalidity()
    {
        if (ServiceType is null || (ImplType is null && Factory is null && OpenGenericFactory is null && Implementation is null))// || (ImplType is not null && ImplType.IsAbstract))
            throw new InvalidOperationException($"Invalid {nameof(IocDescriptor)}");
        
        ThrowIfImplTypeIsNotAssignableToServiceType();
        ThrowIfImplTypeIsNotConstructable();
        
        return this;
    }

    // private bool IsInvalidOpenGeneric() => ServiceType.IsGenericTypeDefinition && (ImplType is null || !ImplType.IsGenericTypeDefinition || Implementation is not null);
    
    private static IocLifetime ThrowOnInvalidLifetime(IocLifetime lifetime)
    {
        if (lifetime is IocLifetime.Scoped or IocLifetime.Singleton or IocLifetime.Transient)
            return lifetime;
        throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, $"Invalid {nameof(IocLifetime)}");
    }

    private readonly IocLifetime _lifetime;
    public required IocLifetime Lifetime
    {
        get => _lifetime;
        init => _lifetime = ThrowOnInvalidLifetime(value);
    }

    public required Type ServiceType { get; init; }
    
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? ImplType { get; init; }
    public object? Implementation { get; init; }
    public Func<IServiceProvider, object>? Factory { get; init; }
    public Func<IServiceProvider, Type[], object>? OpenGenericFactory { get; init; }
    
    // Equality

    public bool Equals(IocDescriptor? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Lifetime == other.Lifetime && ServiceType == other.ServiceType && ImplType == other.ImplType &&
               Equals(Factory, other.Factory) && Equals(OpenGenericFactory, other.OpenGenericFactory) &&
               Equals(Implementation, other.Implementation);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is IocDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return HashCode.Combine((int)Lifetime, ServiceType, ImplType, Factory, OpenGenericFactory, Implementation);
#else
        return ((int)Lifetime, ServiceType, ImplType, Factory, OpenGenericFactory, Implementation).GetHashCode();
#endif
    }

    public static bool operator ==(IocDescriptor? left, IocDescriptor? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(IocDescriptor? left, IocDescriptor? right)
    {
        return !Equals(left, right);
    }
    
    // Conversion

    public static implicit operator IocLifetime(IocDescriptor descriptor) => descriptor.Lifetime;
    
    //
    // Static creation helper:
    
    //
    // Concrete Implementation provided:

    public static IocDescriptor CreateSingleton(Type serviceType, object implementation) => new(serviceType, implementation);
    internal static IocDescriptor CreateScoped(Type serviceType, object implementation) => new(serviceType, implementation, true);
    
    public static IocDescriptor CreateSingleton<T>(T implementation) where T : notnull => new(typeof(T), implementation);
    internal static IocDescriptor CreateScoped<T>(T implementation) where T : notnull => new(typeof(T), implementation, true);
    
    //
    // Factories provided:
    
    public static IocDescriptor CreateSingleton(Type serviceType, Func<IServiceProvider, object> factory) => new(IocLifetime.Singleton, serviceType, factory);
    public static IocDescriptor CreateScoped(Type serviceType, Func<IServiceProvider, object> factory) => new(IocLifetime.Scoped, serviceType, factory);
    public static IocDescriptor CreateTransient(Type serviceType, Func<IServiceProvider, object> factory) => new(IocLifetime.Transient, serviceType, factory);
    
    public static IocDescriptor CreateSingleton<T>(Func<IServiceProvider, T> factory) where T : class => new(IocLifetime.Singleton, typeof(T), factory) { ImplType = ImplTypeWhenConstructable(typeof(T)) }; // ImplType not really needed
    public static IocDescriptor CreateScoped<T>(Func<IServiceProvider, T> factory) where T : class => new(IocLifetime.Scoped, typeof(T), factory) { ImplType = ImplTypeWhenConstructable(typeof(T)) }; // ImplType not really needed
    public static IocDescriptor CreateTransient<T>(Func<IServiceProvider, T> factory) where T : class => new(IocLifetime.Transient, typeof(T), factory) { ImplType = ImplTypeWhenConstructable(typeof(T)) }; // ImplType not really needed
    
    // Open Generic Factories provided:
    
    public static IocDescriptor CreateSingleton(Type serviceType, Func<IServiceProvider, Type, object> openGenericFactory) => new(IocLifetime.Singleton, serviceType, (p, genericArgs) => openGenericFactory.Invoke(p, genericArgs[0]));
    public static IocDescriptor CreateScoped(Type serviceType, Func<IServiceProvider, Type, object> openGenericFactory) => new(IocLifetime.Scoped, serviceType, (p, genericArgs) => openGenericFactory.Invoke(p, genericArgs[0]));
    public static IocDescriptor CreateTransient(Type serviceType, Func<IServiceProvider, Type, object> openGenericFactory) => new(IocLifetime.Transient, serviceType, (p, genericArgs) => openGenericFactory.Invoke(p, genericArgs[0]));
    
    public static IocDescriptor CreateSingleton(Type serviceType, Func<IServiceProvider, Type[], object> openGenericFactory) => new(IocLifetime.Singleton, serviceType, openGenericFactory);
    public static IocDescriptor CreateScoped(Type serviceType, Func<IServiceProvider, Type[], object> openGenericFactory) => new(IocLifetime.Scoped, serviceType, openGenericFactory);
    public static IocDescriptor CreateTransient(Type serviceType, Func<IServiceProvider, Type[], object> openGenericFactory) => new(IocLifetime.Transient, serviceType, openGenericFactory);
    
    //
    // ImplType for Activator: No factory, no concrete Implementation provided:
    
    public static IocDescriptor CreateSingleton(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implType) => new(IocLifetime.Singleton, serviceType, implType);
    public static IocDescriptor CreateScoped(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implType) => new(IocLifetime.Scoped, serviceType, implType);
    public static IocDescriptor CreateTransient(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implType) => new(IocLifetime.Transient, serviceType, implType);
    
    public static IocDescriptor CreateSingleton<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl>() where TImpl : TService => new(IocLifetime.Singleton, typeof(TService), typeof(TImpl));
    public static IocDescriptor CreateScoped<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl>() where TImpl : TService => new(IocLifetime.Scoped, typeof(TService), typeof(TImpl));
    public static IocDescriptor CreateTransient<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl>() where TImpl : TService => new(IocLifetime.Transient, typeof(TService), typeof(TImpl));
    
    //
    // ImplType for Activator with ImplType as ServiceType:
    
    public static IocDescriptor CreateSingleton([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceImplType) => new(IocLifetime.Singleton, serviceImplType, serviceImplType);
    public static IocDescriptor CreateScoped([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceImplType) => new(IocLifetime.Scoped, serviceImplType, serviceImplType);
    public static IocDescriptor CreateTransient([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceImplType) => new(IocLifetime.Transient, serviceImplType, serviceImplType);
    
    public static IocDescriptor CreateSingleton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TServiceImpl>() => new(IocLifetime.Singleton, typeof(TServiceImpl), typeof(TServiceImpl));
    public static IocDescriptor CreateScoped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TServiceImpl>() => new(IocLifetime.Scoped, typeof(TServiceImpl), typeof(TServiceImpl));
    public static IocDescriptor CreateTransient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TServiceImpl>() => new(IocLifetime.Transient, typeof(TServiceImpl), typeof(TServiceImpl));
    
    //
    // Runtime lifetime:
    
    // Concrete Implementation provided:
    
    public static IocDescriptor Create(Type serviceType, object implementation) => new(serviceType, implementation);
    internal static IocDescriptor Create(Type serviceType, object implementation, bool scoped) => new(serviceType, implementation, scoped);
    internal static IocDescriptor Create(IocLifetime lifetime, Type serviceType, object implementation) => new(serviceType, implementation, lifetime is not IocLifetime.Transient ? lifetime is IocLifetime.Scoped : throw new InvalidOperationException());
    
    // " but generic:
    
    internal static IocDescriptor Create<T>(IocLifetime lifetime, T implementation) where T : notnull => new(typeof(T), implementation, lifetime is not IocLifetime.Transient ? lifetime is IocLifetime.Scoped : throw new InvalidOperationException());
    internal static IocDescriptor Create<T>(T implementation, bool scoped) where T : notnull => new(typeof(T), implementation, scoped);
    public static IocDescriptor Create<T>(T implementation) where T : notnull => new(typeof(T), implementation);
    
    // Factories:
    
    public static IocDescriptor Create(IocLifetime lifetime, Type serviceType, Func<IServiceProvider, object> factory) => new(lifetime, serviceType, factory);
    
    public static IocDescriptor Create<T>(IocLifetime lifetime, Func<IServiceProvider, T> factory) where T : class => new(lifetime, typeof(T), factory) { ImplType = ImplTypeWhenConstructable(typeof(T)) }; // ImplType not really needed
    
    public static IocDescriptor Create(IocLifetime lifetime, Type serviceType, Func<IServiceProvider, Type[], object> openGenericFactory) => new(lifetime, serviceType, openGenericFactory);
    public static IocDescriptor Create(IocLifetime lifetime, Type serviceType, Func<IServiceProvider, Type, object> openGenericFactory) => new(lifetime, serviceType, (p, genericArgs) => openGenericFactory.Invoke(p, genericArgs[0]));
    
    // ImplType for Activator:
    
    public static IocDescriptor Create(IocLifetime lifetime, Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implType) => new(lifetime, serviceType, implType);
    
    public static IocDescriptor Create<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImpl>(IocLifetime lifetime) where TImpl : TService => new(lifetime, typeof(TService), typeof(TImpl));
    
    // " with ImplType is ServiceType:
    
    public static IocDescriptor Create(IocLifetime lifetime, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceImplType) => new(lifetime, serviceImplType, serviceImplType);
    
    public static IocDescriptor Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TServiceImpl>(IocLifetime lifetime) => new(lifetime, typeof(TServiceImpl), typeof(TServiceImpl));
}