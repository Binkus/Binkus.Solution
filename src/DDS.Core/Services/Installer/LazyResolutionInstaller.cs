namespace DDS.Core.Services.Installer;

public static class LazyResolutionInstaller
{
    public static IServiceCollection AddLazyResolution(this IServiceCollection services) 
        => services.AddTransient(
            typeof(Lazy<>),
            typeof(LazilyResolved<>));

    private sealed class LazilyResolved<T> : Lazy<T> where T : notnull
    {
        public LazilyResolved(IServiceProvider serviceProvider)
            : base(ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider))
        {
        }
    }
}

