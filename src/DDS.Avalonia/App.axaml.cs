using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
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
        
        // Remove the DataAnnotations validator
        ExpressionObserver.DataValidators.RemoveAll(x => x is DataAnnotationsValidationPlugin);
        
        var locator = Ioc.Default.GetRequiredService<Controls.ViewLocator>();
        DataContext = Ioc.Default.GetRequiredService<ApplicationViewModel>();
        DataTemplates.Add(locator);
        
        var scopeManager = Ioc.Default.GetRequiredService<IServiceScopeManager>();
        var scope = scopeManager.CreateScope();
        var services = scope.ServiceProvider;
        
        var appVmPerfLog = time.LogTime<PerformanceLogger.AppViewModelCreationAndSetPerformance>().Save();
        
        time = 0.AddTimestamp();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = services.GetRequiredService<MainWindow>();
            desktop.MainWindow.Height = 960;
            desktop.MainWindow.Width = 690;
            
            // WindowSpawnHelper.SpawnMainWindow(); // devOnly: test spawning multiple MainWindows without reason
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = services.GetRequiredService<MainView>();
        }
        
        var mainVMsPerfLog = time.LogTime<PerformanceLogger.MainViewsViewModelsStartupPerformance>().Save();

        LogPerformance(appVmPerfLog, mainVMsPerfLog);

        base.OnFrameworkInitializationCompleted();
    }

    // [Conditional("DEBUG")]
    private static void LogPerformance(
        scoped in PerformanceLogger.DurationLogEntry appPerf, 
        scoped in PerformanceLogger.DurationLogEntry mainPerf)
    {
        TimeSpan notAvaloniaTime = PerformanceLogger.GetNotFrameworkInitPerformance();
        
        if (PerformanceLogger.ClearLogsAfterInit) PerformanceLogger.ClearLogs();
        
        notAvaloniaTime.LogTime<PerformanceLogger.TotalAppWithoutFrameworkStartupPerformance>();
        
        var total = Startup.StartTimestamp.LogTime<PerformanceLogger.TotalAppStartupPerformance>(false);
        
        notAvaloniaTime.Subtract(appPerf).Subtract(mainPerf)
            .LogTime<PerformanceLogger.TotalAppWithoutFrameworkWithoutInitVmsStartupPerformance>();
        
        total.TimeSpan.Subtract(notAvaloniaTime).LogTime<PerformanceLogger.AvaloniaStartupPerformance>();

        total.Print();
    }
    

    // public void Post(Action action)
    // {
    //     Dispatcher.UIThread.Post(action);
    // }
}