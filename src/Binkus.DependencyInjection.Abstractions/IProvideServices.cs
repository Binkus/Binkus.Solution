using Microsoft.Extensions.DependencyInjection;

namespace Binkus.DependencyInjection;

public interface IProvideServices : IServiceProvider
{
    IServiceProvider Services { get; }
    
    object? IServiceProvider.GetService(Type serviceType) => Services.GetService(serviceType);
}

public static class ProvideServicesExtensions
{
    // required cause default interface implementation of IServiceProvider IServiceProvider.GetService(Type) gets hidden
    /// <inheritdoc cref="IServiceProvider.GetService"/>
    public static object? GetService(this IProvideServices serviceProviderProvider, Type serviceType)
        => serviceProviderProvider.Services.GetService(serviceType);
    
    // not required
    //
    
    /// <summary><inheritdoc cref="ServiceProviderServiceExtensions.GetService{T}"/></summary>
    /// <param name="serviceProviderProvider">The <see cref="IProvideServices"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <returns><inheritdoc cref="ServiceProviderServiceExtensions.GetService{T}"/></returns>
    /// <exception cref="System.InvalidOperationException">
    /// <inheritdoc cref="ServiceProviderServiceExtensions.GetService{T}"/></exception>
    public static TService? GetService<TService>(this IProvideServices serviceProviderProvider)
        => serviceProviderProvider.Services.GetService<TService>();
    
    //
    
    /// <summary><inheritdoc cref="ServiceProviderServiceExtensions.GetRequiredService"/></summary>
    /// <param name="serviceProviderProvider">The <see cref="IProvideServices"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <param name="serviceType"><inheritdoc cref="ServiceProviderServiceExtensions.GetRequiredService"/></param>
    /// <returns><inheritdoc cref="ServiceProviderServiceExtensions.GetRequiredService"/></returns>
    /// <exception cref="System.InvalidOperationException">
    /// <inheritdoc cref="ServiceProviderServiceExtensions.GetRequiredService"/></exception>
    public static object GetRequiredService(this IProvideServices serviceProviderProvider, Type serviceType)
        => serviceProviderProvider.Services.GetRequiredService(serviceType);

    /// <summary><inheritdoc cref="ServiceProviderServiceExtensions.GetRequiredService{T}"/></summary>
    /// <param name="serviceProviderProvider">The <see cref="IProvideServices"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <returns><inheritdoc cref="ServiceProviderServiceExtensions.GetRequiredService{T}"/></returns>
    /// <exception cref="System.InvalidOperationException">
    /// <inheritdoc cref="ServiceProviderServiceExtensions.GetRequiredService{T}"/></exception>
    public static TService GetRequiredService<TService>(this IProvideServices serviceProviderProvider)
        where TService : notnull => serviceProviderProvider.Services.GetRequiredService<TService>();
}