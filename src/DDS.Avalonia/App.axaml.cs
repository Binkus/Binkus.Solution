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
        
        time.LogTime("Set App DataContext and create scope");
        var scope = scopeManager.CreateScope();
        var services = scope.ServiceProvider;
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var time2 = 0.AddTimestamp();
            desktop.MainWindow = services.GetRequiredService<MainWindow>();
            time2.LogTime("MainWindow with MainView creation time");

            desktop.MainWindow.Height = 960;
            desktop.MainWindow.Width = 690;
            
            // WindowSpawnHelper.SpawnMainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var time2 = 0.AddTimestamp();

            // services = Globals.Services; // as long as no ScopeManagerService exists
            singleViewPlatform.MainView = services.GetRequiredService<MainView>();
            
            time2.LogTime("MainView creation time");
        }

        TimeSpan notAvaloniaTime = TimeSpan.Zero;
        PerformanceLogger.PerformanceLogs.Values.ForEach(x => notAvaloniaTime = notAvaloniaTime.Add(x));
        notAvaloniaTime.AddTimestamp().LogTime("Total app startup time without Avalonia Framework time but with first view / MainWindow creation time");
        Startup.StartTimestamp.LogTime("Total App Startup time");
        var total = PerformanceLogger.TryGetResult("Total App Startup time")!.Value;
        total.Subtract(notAvaloniaTime).AddTimestamp().LogTime("Avalonia Startup time (already in total startup time), without first view / window creation time");
        PerformanceLogger.ClearLogs();

        base.OnFrameworkInitializationCompleted();
    }

    public void Post(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }
}