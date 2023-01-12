using System.Diagnostics.CodeAnalysis;
using Binkus.DependencyInjection.Extensions;

namespace Binkus.DependencyInjection;

/// <summary>
/// Additional extension methods for IServiceProvider
/// </summary>
public static class ServiceProviderServiceExtensions
{
    public static TServiceToBeAssignableTo? TryGetServiceOrCreateInstance<TServiceToBeAssignableTo>
    (this IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type)
    {
        if (type is null || !type.IsAssignableTo(typeof(TServiceToBeAssignableTo))) return default;
        
        if (!type.IsAbstract) 
            return (TServiceToBeAssignableTo?)services.GetIocUtilities().GetServiceOrCreateInstance(services, type);
        
        return (TServiceToBeAssignableTo?)services.GetService(type);
    }

    public static TServiceToBeAssignableTo? TryGetServiceOrCreateInstance
        <TServiceToBeAssignableTo,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TServiceToGetOrCreate>
        (this IServiceProvider services) where TServiceToGetOrCreate : TServiceToBeAssignableTo
    {
        return !typeof(TServiceToGetOrCreate).IsAbstract
            ? services.GetIocUtilities().GetServiceOrCreateInstance<TServiceToGetOrCreate>(services)
            : services.GetService<TServiceToGetOrCreate>();
    }

    public static TService? TryGetServiceOrCreateInstance<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>
        (this IServiceProvider services)
    {
        return !typeof(TService).IsAbstract
            ? services.GetIocUtilities().GetServiceOrCreateInstance<TService>(services)
            : services.GetService<TService>();
    }
    
    //
    
    public static object? TryGetServiceOrCreateInstance(this IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type,
        Type? serviceTypeToBeAssignableTo)
    {
        if (type is null || !type.IsAssignableTo(serviceTypeToBeAssignableTo)) return default;

        return !type.IsAbstract
            ? services.GetIocUtilities().GetServiceOrCreateInstance(services, type)
            : services.GetService(type);
    }
    
    public static object? TryGetServiceOrCreateInstance(this IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type)
    {
        if (type is null ) return default;

        return !type.IsAbstract
            ? services.GetIocUtilities().GetServiceOrCreateInstance(services, type)
            : services.GetService(type);
    }
    
    //
    
    public static object? TryCreateInstance(this IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type,
        Type? serviceTypeToBeAssignableTo, params object[] parameters)
    {
        if (type is null || !type.IsAssignableTo(serviceTypeToBeAssignableTo)) return default;

        return !type.IsAbstract
            ? services.GetIocUtilities().CreateInstance(services, type, parameters)
            : default;
    }
    
    public static object? TryCreateInstance(this IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type,
        params object[] parameters)
    {
        if (type is null ) return default;

        return !type.IsAbstract
            ? services.GetIocUtilities().CreateInstance(services, type, parameters)
            : default;
    }
    
    //
}