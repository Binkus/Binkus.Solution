namespace DDS.Core.Services;

public interface IProvideServices
{
    IServiceProvider Services { get; }

    TService GetService<TService>() where TService : notnull;
    
    object GetService(Type serviceType);
}

// public static class ProvideServicesExtensions
// {
//     public static TService GetService<TService>(this IProvideServices serviceProviderProvider) where TService : notnull
//         => serviceProviderProvider.Services.GetRequiredService<TService>();
//     
//     public static object GetService(this IProvideServices serviceProviderProvider, Type serviceType)
//         => serviceProviderProvider.Services.GetRequiredService(serviceType);
// }