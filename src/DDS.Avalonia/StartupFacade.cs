using DDS.Core.ViewModels;

namespace DDS.Avalonia;

public static class StartupFacade
{
    internal static void ConfigureServices(IServiceCollection services)
    {
        
    }
    
    internal static void ConfigureViewViewModels(IServiceCollection services)
    {
        services.AddViewAndViewModels<TestView, TestViewModel>(ServiceLifetime.Singleton);
        services.AddViewAndViewModels<SecondTestView, SecondTestViewModel>();
        services.AddViewAndViewModels<ThirdTestView, ThirdTestViewModel>(ServiceLifetime.Transient);
    }
    
    internal static void ConfigureWindowViewModels(IServiceCollection services)
    {
        
    }
}