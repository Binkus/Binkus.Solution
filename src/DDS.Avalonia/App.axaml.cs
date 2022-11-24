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
        
        // Task.Run(() => Globals.Services.GetRequiredService<ApplicationViewModel>())
        //     .ContinueWith(vm => Dispatcher.UIThread.InvokeAsync(
        //         async () => DataContext = await vm, DispatcherPriority.Background));
        DataContext = Globals.Services.GetRequiredService<ApplicationViewModel>();
        
        var scope = Globals.Services.CreateScope();
        var services = scope.ServiceProvider;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = services.GetRequiredService<MainWindow>();
            desktop.MainWindow.Height = 960;
            desktop.MainWindow.Width = 690;
            
            // Spawn another MainWindow for scope testing

            // Task.Run(async () =>
            // {
            //     await 4.Seconds();
            //     
            //     Dispatcher.UIThread.Post(() =>
            //     {
            //         sw = Stopwatch.StartNew();
            //         var window2 = Globals.Services.CreateAsyncScope().ServiceProvider.GetRequiredService<MainWindow>();
            //         Console.WriteLine(sw.ElapsedMilliseconds);
            //         window2.Show();
            //
            //         window2.Height = 960;
            //         window2.Width = 690;
            //     }, DispatcherPriority.Background);
            // });
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            services = Globals.Services; // as long as no ScopeManagerService exists
            singleViewPlatform.MainView = services.GetRequiredService<MainView>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}