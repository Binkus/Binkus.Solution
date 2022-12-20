using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DDS.Avalonia.Helper;
using DDS.Core;
using DDS.Core.Helper;
using DDS.Core.Services;

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
        
        var time = 0.AddTimestamp();
        
        DataContext = Globals.Services.GetRequiredService<ApplicationViewModel>();
        var scopeManager = Globals.Services.GetRequiredService<ServiceScopeManager>();
        
        time.LogTime<PerformanceLogger.AppViewModelCreationAndSetPerformance>().Save();
        var scope = scopeManager.CreateScope();
        var services = scope.ServiceProvider;
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var time2 = 0.AddTimestamp();
            
            desktop.MainWindow = services.GetRequiredService<MainWindow>();
            desktop.MainWindow.Height = 960;
            desktop.MainWindow.Width = 690;
            
            time2.LogTime<PerformanceLogger.MainViewsViewModelsStartupPerformance>().Save();

            // WindowSpawnHelper.SpawnMainWindow(); // devOnly: test spawning multiple MainWindows without reason
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var time2 = 0.AddTimestamp();
            
            singleViewPlatform.MainView = services.GetRequiredService<MainView>();
            
            time2.LogTime<PerformanceLogger.MainViewsViewModelsStartupPerformance>().Save();
        }

        TimeSpan notAvaloniaTime = TimeSpan.Zero;
        PerformanceLogger.PerformanceLogs.Values.ForEach(x => notAvaloniaTime = notAvaloniaTime.Add(x));
        PerformanceLogger.ClearLogs();
        notAvaloniaTime.LogTime<PerformanceLogger.TotalAppWithoutFrameworkStartupPerformance>();
        
        var total = Startup.StartTimestamp.LogTime<PerformanceLogger.TotalAppStartupPerformance>(false);
        total.TimeSpan.Subtract(notAvaloniaTime).LogTime<PerformanceLogger.AvaloniaStartupPerformance>();
        total.Print();

        base.OnFrameworkInitializationCompleted();
    }

    public void Post(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }
}