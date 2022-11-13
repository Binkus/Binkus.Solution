namespace DDS;

public static class StartupFacade
{
    internal static void ConfigureServices(IServiceCollection services)
    {
        
    }
    
    internal static void ConfigureViewViewModels(IServiceCollection services)
    {
        services.AddViewAndViewModels<TestView, TestViewModel>(ServiceLifetime.Singleton);
        services.AddViewAndViewModels<SecondTestView, SecondTestViewModel>(ServiceLifetime.Transient);
    }
    
    internal static void ConfigureWindowViewModels(IServiceCollection services)
    {
        
    }
}