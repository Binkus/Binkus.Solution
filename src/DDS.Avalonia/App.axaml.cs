using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DDS.Core;

namespace DDS.Avalonia;

public sealed partial class App : Application, IAppCore
{
    // ctor DI on (only) this 'View' is not supported, cause created by Avalonia without any DI involved
    // and Global ServiceProvider has not been constructed yet.
    [UsedImplicitly] public App() {}

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // Global ServiceProvider not yet set.
    }

    /// <summary>
    /// Avalonia, ReactiveUI, Startup and Globals are fully initialized. 
    /// Sets initial MainView (or MainView wrapped in MainWindow if Desktop)
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        // Global ServiceProvider available:
        DataContext = Globals.Services.GetRequiredService<ApplicationViewModel>();
        // var scope = Globals.Services.CreateScope();
        // var services = scope.ServiceProvider;
        var services = Globals.Services; // we use default scope for first instances of Main*
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = services.GetRequiredService<MainWindow>();
            desktop.MainWindow.Height = 960;
            desktop.MainWindow.Width = 690;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = services.GetRequiredService<MainView>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}