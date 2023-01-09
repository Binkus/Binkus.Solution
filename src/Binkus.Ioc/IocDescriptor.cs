using System.Diagnostics.CodeAnalysis;

// ReSharper disable MemberCanBePrivate.Global

namespace Binkus.DependencyInjection;

public delegate object ServiceFactory(IServiceProvider services);

public sealed class IocDescriptor : IEquatable<IocDescriptor>
{
    [SetsRequiredMembers]
    public IocDescriptor(
        IocLifetime lifetime,
        Type serviceType, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implType);
        Lifetime = lifetime;
        ServiceType = serviceType;
        ImplType = implType;
    }
    
    [SetsRequiredMembers]
    public IocDescriptor(IocLifetime lifetime, Type serviceType, ServiceFactory factory)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(factory);
        Lifetime = lifetime;
        ServiceType = serviceType;
        ImplFactory = factory;
    }
    
    [SetsRequiredMembers]
    public IocDescriptor(Type serviceType, object implementation)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementation);
        Lifetime = IocLifetime.Singleton;
        ServiceType = serviceType;
        // ImplType = implementation.GetType();
        Implementation = implementation;
    }
    
    [SetsRequiredMembers]
    public IocDescriptor(IocDescriptor descriptor)
    {
        ServiceType = descriptor.ServiceType;
        ImplType = descriptor.ImplType;
        Lifetime = descriptor.Lifetime;
        ImplFactory = descriptor.ImplFactory;
        // Factory = descriptor.Factory; // don't cause descriptor.Factory captures property Implementation of param. descriptor 
    }

    internal IocDescriptor() { }

    internal IocDescriptor CreateForScope() => new(this);

    public required IocLifetime Lifetime { get; init; }
    public required Type ServiceType { get; init; }
    
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type? ImplType { get; init; } // potentially: ?? Implementation?.GetType();
    internal object? Implementation { get; set; }

    private readonly ServiceFactory? _implFactory;
    public ServiceFactory? ImplFactory
    {
        get => _implFactory;
        init
        {
            Factory = _implFactory = value;
            if (value is null || Lifetime is IocLifetime.Transient) return;
            Factory = services =>
            {
                // if (Implementation is not null) return Implementation;
                lock (this) return Implementation ??= value.Invoke(services);
                
                // ImplType = implementation.GetType();
            };
        }
    }

    internal ServiceFactory? Factory { get; init; }
    
    internal IocDescriptor? Next { get; set; }
    
    // Equality: (x is equal to y when every field (except ignored Implementation) is equal)
    // Implementation is ignored, because the Descriptor of root container shall be equivalent
    // to the one of a scoped container, independent from if Implementation has been cached or created differently or not 

    public bool Equals(IocDescriptor? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Lifetime == other.Lifetime && ServiceType == other.ServiceType && ImplType == other.ImplType &&
               Equals(ImplFactory, other.ImplFactory);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is IocDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Lifetime, ServiceType, ImplType, ImplFactory);
    }

    public static bool operator ==(IocDescriptor? left, IocDescriptor? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(IocDescriptor? left, IocDescriptor? right)
    {
        return !Equals(left, right);
    }
}