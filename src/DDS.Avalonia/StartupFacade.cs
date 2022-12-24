using DDS.Core.Services;
using DDS.Core.Services.Installer;
using DDS.Core.ViewModels;

namespace DDS.Avalonia;

public static class StartupFacade
{
    internal static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILoginService, LoginService>();
    }
    
    internal static void ConfigureViewViewModels(IServiceCollection services)
    {
        services.AddViewViewModel<LoginView, LoginViewModel>();
        services.AddViewViewModel<TestView, TestViewModel>(ServiceLifetime.Singleton);
        services.AddViewViewModel<SecondTestView, SecondTestViewModel>();
        services.AddViewViewModel<ThirdTestView, ThirdTestViewModel>(ServiceLifetime.Transient);
    }
    
    internal static void ConfigureWindowViewModels(IServiceCollection services)
    {
        
    }
}