using Microsoft.Extensions.DependencyInjection;

namespace Binkus.DependencyInjection;

public static class DiConfig
{
    public static void SetMsDiActivatorUtilitiesForIocUtilitiesDelegation()
    {
        var d = IocUtilitiesDelegation.Default;
        d.FuncCreateInstance = ActivatorUtilities.CreateInstance;
        d.FuncGetServiceOrCreateInstance = ActivatorUtilities.GetServiceOrCreateInstance;
    }
}