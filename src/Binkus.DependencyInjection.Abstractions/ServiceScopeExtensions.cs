using Microsoft.Extensions.DependencyInjection;

namespace Binkus.DependencyInjection;

public static class ServiceScopeExtensions
{
    /// <summary><inheritdoc cref="IServiceProvider.GetService(Type)"/></summary>
    /// <param name="serviceScope">The <see cref="IServiceScope"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <param name="serviceType"><inheritdoc cref="IServiceProvider.GetService(Type)"/></param>
    /// <returns><inheritdoc cref="IServiceProvider.GetService(Type)"/></returns>
    public static object? GetService<TScope>(this TScope serviceScope, Type serviceType) where TScope : IServiceScope
        => serviceScope.ServiceProvider.GetService(serviceType);
    
    /// <summary><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService{T}"/></summary>
    /// <typeparam name="TService">The type of service object to get.</typeparam>
    /// <param name="serviceScope">The <see cref="IServiceScope"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <returns><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService{T}"/></returns>
    public static TService? GetService<TService>(this IServiceScope serviceScope)
        => serviceScope.ServiceProvider.GetService<TService>();
    
    /// <inheritdoc cref="GetService{TServvice}(IServiceScope)"/>
    public static TService? GetService<TService>(this AsyncServiceScope serviceScope)
        => serviceScope.ServiceProvider.GetService<TService>();
    
    /// <inheritdoc cref="GetService{TServvice}(IServiceScope)"/>
    public static TService? GetService<TService, TScope>(this TScope serviceScope) where TScope : IServiceScope
        => serviceScope.ServiceProvider.GetService<TService>();
    
    //
    
    /// <summary><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService"/></summary>
    /// <param name="serviceScope">The <see cref="IServiceScope"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <param name="serviceType">An object that specifies the type of service object to get.</param>
    /// <returns><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService"/></returns>
    /// <exception cref="System.InvalidOperationException">
    /// <inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService"/></exception>
    public static object GetRequiredService<TScope>(this TScope serviceScope, Type serviceType) where TScope : IServiceScope
        => serviceScope.ServiceProvider.GetRequiredService(serviceType);

    /// <summary><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService{T}"/></summary>
    /// <typeparam name="TService">The type of service object to get.</typeparam>
    /// <param name="serviceScope">The <see cref="IServiceScope"/> to retrieve the
    /// <see cref="IServiceProvider"/> object from to retrieve the service object from.</param>
    /// <returns><inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService{T}"/></returns>
    /// <exception cref="System.InvalidOperationException">
    /// <inheritdoc cref="Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService{T}"/></exception>
    public static TService GetRequiredService<TService>(this IServiceScope serviceScope)
        where TService : notnull => serviceScope.ServiceProvider.GetRequiredService<TService>();
    
    /// <inheritdoc cref="GetRequiredService{TServvice}(IServiceScope)"/>
    public static TService GetRequiredService<TService>(this AsyncServiceScope serviceScope)
        where TService : notnull => serviceScope.ServiceProvider.GetRequiredService<TService>();
    
    /// <inheritdoc cref="GetRequiredService{TServvice}(IServiceScope)"/>
    public static TService GetRequiredService<TService, TScope>(this TScope serviceScope) 
        where TService : notnull where TScope : IServiceScope
        => serviceScope.ServiceProvider.GetRequiredService<TService>();
}