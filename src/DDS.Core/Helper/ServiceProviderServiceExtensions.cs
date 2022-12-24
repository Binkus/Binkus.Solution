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
        return !typeof(TServiceToGetOrCreate).IsAbstract
            ? ActivatorUtilities.GetServiceOrCreateInstance<TServiceToGetOrCreate>(services)
            : services.GetService<TServiceToGetOrCreate>();
    }

    public static TService? TryGetServiceOrCreateInstance<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>
        (this IServiceProvider services)
    {
        return !typeof(TService).IsAbstract
            ? ActivatorUtilities.GetServiceOrCreateInstance<TService>(services)
            : services.GetService<TService>();
    }
}