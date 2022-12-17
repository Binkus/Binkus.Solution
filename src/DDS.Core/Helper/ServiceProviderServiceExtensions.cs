namespace DDS.Core.Helper;

public static class ServiceProviderServiceExtensions
{
    public static TServiceToBeAssignableTo? TryGetServiceOrCreateInstance<TServiceToBeAssignableTo>
    (this IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type)
    {
        if (type is null || !type.IsAssignableTo(typeof(TServiceToBeAssignableTo))) return default;
        
        if (!type.IsAbstract) 
            return (TServiceToBeAssignableTo?)ActivatorUtilities.GetServiceOrCreateInstance(services, type);
        
        return (TServiceToBeAssignableTo?)services.GetService(type);
    }

    public static TServiceToBeAssignableTo? TryGetServiceOrCreateInstance
        <TServiceToBeAssignableTo,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TServiceToGetOrCreate>
        (this IServiceProvider services) where TServiceToGetOrCreate : TServiceToBeAssignableTo
    {
        // if (!typeof(TServiceToGetOrCreate).IsAssignableTo(typeof(TServiceToBeAssignableTo))) return default;
        
        if (!typeof(TServiceToGetOrCreate).IsAbstract)
            return ActivatorUtilities.GetServiceOrCreateInstance<TServiceToGetOrCreate>(services);
        
        return services.GetService<TServiceToGetOrCreate>();
    }

    public static TService? TryGetServiceOrCreateInstance<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>
        (this IServiceProvider services)
    {
        if (!typeof(TService).IsAbstract)
            return (TService?)ActivatorUtilities.GetServiceOrCreateInstance<TService>(services);
        
        return services.GetService<TService>();
    }
}