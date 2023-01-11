namespace Binkus.DependencyInjection.Extensions;

// in separate namespace on purpose to prevent potential ambiguity when using together
// with Microsoft.Extensions.DependencyInjection

/// <summary>
/// Basic extension methods for IServiceProvider
/// </summary>
public static class ServiceProviderExtensions
{
    public static T? GetService<T>(this IServiceProvider services) => (T?)services.GetService(typeof(T));
    
    public static T GetRequiredService<T>(this IServiceProvider services) 
        => (T)(services.GetService(typeof(T)) ?? throw new InvalidOperationException());
    
    public static object GetRequiredService(this IServiceProvider services, Type serviceType) 
        => services.GetService(serviceType) ?? throw new InvalidOperationException();
}