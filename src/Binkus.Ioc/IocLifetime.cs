namespace Binkus.DependencyInjection;

/// <summary>
/// Specifies the lifetime of a service described as <see cref="T:Binkus.DependencyInjection.IocDescriptor" />
/// for an <see cref="T:Binkus.DependencyInjection.IocContainerScope" />.
/// </summary>
public enum IocLifetime
{
    /// <summary>
    /// Specifies that a single instance of the service will be created.
    /// </summary>
    Singleton,
    
    /// <summary>
    /// Specifies that a new instance of the service will be created for each scope.
    /// </summary>
    /// <remarks>
    /// In Binkus' GUI templates, a scope is created for each window. 
    /// <para />
    /// In ASP.NET Core applications a scope is created around each server request.
    /// </remarks>
    Scoped,
    
    /// <summary>
    /// Specifies that a new instance of the service will be created every time it is requested.
    /// </summary>
    Transient,
}