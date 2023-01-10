using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Binkus.DependencyInjection;

public static class IocMapper
{
    public static IocLifetime ToLifetime(this ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Singleton => IocLifetime.Singleton,
            ServiceLifetime.Scoped => IocLifetime.Scoped,
            ServiceLifetime.Transient => IocLifetime.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };
    
    public static ServiceLifetime ToLifetime(this IocLifetime lifetime) =>
        lifetime switch
        {
            IocLifetime.Singleton => ServiceLifetime.Singleton,
            IocLifetime.Scoped => ServiceLifetime.Scoped,
            IocLifetime.Transient => ServiceLifetime.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };


    public static IocDescriptor ToDescriptor(this ServiceDescriptor d) =>
        new IocDescriptor
        {
            Lifetime = d.Lifetime.ToLifetime(),
            ServiceType = d.ServiceType,
            ImplType = d.ImplementationType,
            Factory = d.ImplementationFactory,
            Implementation = d.ImplementationInstance,
        }.ThrowOnInvalidity();

    public static ServiceDescriptor ToDescriptor(this IocDescriptor d)
    {
        if (d.Factory != null)
            return ServiceDescriptor.Describe(d.ServiceType, d.Factory, d.Lifetime.ToLifetime());
        if (d.Implementation != null)
            return d.Lifetime is IocLifetime.Singleton
                ? ServiceDescriptor.Singleton(d.ServiceType, d.Implementation)
                : ServiceDescriptor.Scoped(d.ServiceType, _ => d.Implementation);
        if (d.ImplType != null)
            return ServiceDescriptor.Describe(d.ServiceType, d.ImplType, d.Lifetime.ToLifetime());
        
        throw new InvalidOperationException("Can't map IocDescriptor to ServiceDescriptor");
    }

    public static ServiceCollection ToServiceCollection(this IEnumerable<IocDescriptor> descriptors)
    {
        var serviceCollection = new ServiceCollection();
        foreach (var d in descriptors)
        {
            serviceCollection.Add(d.ToDescriptor());
        }
        return serviceCollection;
    }
    
    public static List<IocDescriptor> ToServiceCollection(this IEnumerable<ServiceDescriptor> descriptors)
    {
        return descriptors.Select(d => d.ToDescriptor()).ToList();
    }
}