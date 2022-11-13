using System.Linq.Expressions;
using System.Reflection;

namespace DDS.Services;

public interface IProvideServices
{
    IServiceProvider Services { get; }

    TService GetService<TService>() where TService : notnull;
    
    object GetService(Type serviceType);
}

// public static class ServicesExtensions
// {
    // public static object? GetServices(this IProvideServices services, Delegate func)
    // {
    //     var createdParameters = func.Method.GetParameters().Select(parameterInfo =>
    //         ActivatorUtilities.GetServiceOrCreateInstance(services.Services, parameterInfo.ParameterType)).ToArray();
    //     return func.DynamicInvoke(createdParameters);
    // }

    // public static TService GetService<TService>(this IServices services) where TService : notnull 
    //     => services.Services.GetRequiredService<TService>();
    //
    // public static object GetService(this IServices services, Type serviceType)
    //     => services.Services.GetRequiredService(serviceType);


    // static void Test()
    // {
    //     GetServices(default!, Test);
    // }
    //
    // static void Test(bool a)
    // {
    //     
    // }
    
    
    // public static (Task<T0>, Task<T1>, Task<T2>, Task<T3>) GetServices<T0, T1, T2, T3>(this IProvideServices provider)
    //     where T0 : notnull 
    //     where T1 : notnull
    //     where T2 : notnull
    //     where T3 : notnull
    // {
    //     var t0 = Task.Run(provider.GetService<T0>);
    //     var t1 = Task.Run(provider.GetService<T1>);
    //     var t2 = Task.Run(provider.GetService<T2>);
    //     var t3 = Task.Run(provider.GetService<T3>);
    //     return (t0,t1,t2,t3);
    // }
// }