namespace Binkus.DependencyInjection;

public static class SetupIocUtilities
{
    public static void SetIocUtilitiesForIocUtilitiesDelegation(IServiceProvider services)
    {
        var d = services.GetIocUtilities();
        d.FuncCreateInstance = IocUtilities.CreateInstance;
        d.FuncGetServiceOrCreateInstance = IocUtilities.GetServiceOrCreateInstance;
    }
    
    // public static IList<IocDescriptor> SetIocUtilitiesForIocUtilitiesDelegation(
    //     this IList<IocDescriptor> services)
    //     => services.AddSingleton(
    //         new IocUtilitiesDelegation(IocUtilities.CreateInstance, IocUtilities.GetServiceOrCreateInstance)
    //     );
}