namespace Binkus.Ioc.Extensions;

public static class ServiceProviderExtensions
{
    public static T? GetService<T>(this IServiceProvider services) => (T?)services.GetService(typeof(T));
    
    public static T GetRequiredService<T>(this IServiceProvider services) 
        => (T)(services.GetService(typeof(T)) ?? throw new InvalidOperationException());
    
    public static object GetRequiredService(this IServiceProvider services, Type serviceType) 
        => services.GetService(serviceType) ?? throw new InvalidOperationException();
}