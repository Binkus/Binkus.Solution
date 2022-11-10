namespace DDS;

public static class StartupFacade
{
    internal static void ConfigureServices(IServiceCollection services)
    {
        
    }
    
    internal static void ConfigureViewViewModels(IServiceCollection services)
    {
        services.AddViewAndViewModels<SecondTestView, SecondTestViewModel>(ServiceLifetime.Singleton);
        services.AddViewAndViewModels<TestView, TestViewModel>(ServiceLifetime.Singleton);
    }
    
    internal static void ConfigureWindowViewModels(IServiceCollection services)
    {
        
    }
}