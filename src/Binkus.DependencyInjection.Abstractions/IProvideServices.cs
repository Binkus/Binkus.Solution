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
    
    //
    // not required (for simpler access of those basic functions with just one using Binkus.DependencyInjection):
    
    /// <summary><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService{T}"/></summary>
    /// <param name="serviceProviderProvider">The <see cref="IProvideServices"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <returns><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService{T}"/></returns>
    /// <exception cref="System.InvalidOperationException">
    /// <inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService{T}"/></exception>
    public static TService? GetService<TService>(this IProvideServices serviceProviderProvider)
        => serviceProviderProvider.Services.GetService<TService>();
    
    //
    
    /// <summary><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService"/></summary>
    /// <param name="serviceProviderProvider">The <see cref="IProvideServices"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <param name="serviceType"><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService"/></param>
    /// <returns><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService"/></returns>
    /// <exception cref="System.InvalidOperationException">
    /// <inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService"/></exception>
    public static object GetRequiredService(this IProvideServices serviceProviderProvider, Type serviceType)
        => serviceProviderProvider.Services.GetRequiredService(serviceType);

    /// <summary><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService{T}"/></summary>
    /// <param name="serviceProviderProvider">The <see cref="IProvideServices"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <returns><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService{T}"/></returns>
    /// <exception cref="System.InvalidOperationException">
    /// <inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService{T}"/></exception>
    public static TService GetRequiredService<TService>(this IProvideServices serviceProviderProvider)
        where TService : notnull => serviceProviderProvider.Services.GetRequiredService<TService>();
}