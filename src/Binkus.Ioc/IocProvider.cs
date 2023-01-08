namespace Binkus.DependencyInjection;

public delegate object? GetService(Type serviceType);

public sealed class IocProviderWrapper : IServiceProvider
{
    private readonly GetService _getService;

    public IocProviderWrapper(IServiceProvider serviceProvider) => _getService = serviceProvider.GetService;
    public IocProviderWrapper(GetService getService) => _getService = getService;

    // object? IServiceProvider.GetService(Type serviceType) => _getService(serviceType);
    public object? GetService(Type serviceType) => _getService(serviceType);
}

public static class IocProviderExt
{
    // public static object? GetService(this IocProvider provider, Type serviceType)
    //     => ((IServiceProvider)provider).GetService(serviceType);
}