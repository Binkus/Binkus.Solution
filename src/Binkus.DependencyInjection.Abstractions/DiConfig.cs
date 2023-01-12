using Microsoft.Extensions.DependencyInjection;

namespace Binkus.DependencyInjection;

public static class DiConfig
{
    public static void SetMsDiActivatorUtilitiesForIocUtilitiesDelegation(IServiceProvider services)
    {
        var d = services.GetIocUtilities();
        d.FuncCreateInstance = ActivatorUtilities.CreateInstance;
        d.FuncGetServiceOrCreateInstance = ActivatorUtilities.GetServiceOrCreateInstance;
    }

    public static IServiceCollection SetMsDiActivatorUtilitiesForIocUtilitiesDelegation(
        this IServiceCollection services)
        => services.AddSingleton(
            new IocUtilitiesDelegation(ActivatorUtilities.CreateInstance, ActivatorUtilities.GetServiceOrCreateInstance)
            );
}