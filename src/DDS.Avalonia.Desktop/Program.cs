using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Dialogs;
using DDS.Avalonia.Controls.ApplicationLifetimes;
using DDS.Avalonia.Desktop.Controls;
using DDS.Avalonia.Desktop.Services;
using DDS.Avalonia.Services;
using DDS.Avalonia.Views;
using DDS.Core;
using DDS.Core.Controls;
using DDS.Core.Helper;
using DDS.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using TaskExtensions = DDS.Core.Helper.TaskExtensions;

namespace DDS.Avalonia.Desktop
{
    file static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break. (:
        [STAThread]
        private static async Task<int> Main(string[] args) => await BuildAvaloniaApp()
            .StartAsync(args).ConfigureAwait(false)
            // .Inject(b => StartStuff(b, args))
            // .StartWithClassicDesktopLifetime(args)
        ;

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .Inject(DoSomething)
                .ConfigureAppServices(services => services
                        .AddSingleton<ICloseAppService,CloseAppService>()
                        .AddScoped<IDialogAlertMessageBox,DialogAlertMessageBox>()
                    )
                .UsePlatformDetect()
                .LogToTrace();
        
        // todo mechanism to check if an instance is already running with optional additional features:
        // options:
        // block new instances -> show already created one
        // create completely new one (singletons and statics not shared) - dangerous
        // create new window with optionally - recommended - new Service Scope (only singletons and static shared)
        // create new virtual window in same MainWindow like in a single view application (not yet fully implemented),
        // -> basically new service scope with shared singletons and static, like with new window with new scope
        // -> but switchable / exchangeable between one window (makes mostly only sense for Mobile SingleViewApps)

        private static void DoSomething()
        {
            
        }

        private static async Task<int> StartAsync(this AppBuilder builder, string[] args)
        {
            if (false)
                builder.UseManagedSystemDialogs(); // non-native - Avalonia - e.g. FileDialogs

            using var lifetime = new ReactiveClassicDesktopStyleApplicationLifetime(args);
            // using var lifetime = new ClassicDesktopStyleApplicationLifetime
            //     { ShutdownMode = ShutdownMode.OnLastWindowClose, Args = args };
            builder.SetupWithLifetime(lifetime);
            // var exitCode = lifetime.Start(args);
            var exitCode = (await lifetime.StartAsync(args)).exitCode;
            // for (int i = 0; i < 10; i++)
            // {
            //     Console.WriteLine($"ExitCode:{exitCode}");
            //     lifetime.MainWindow = Globals.GetService<ServiceScopeManager>().ReplaceMainScope(true)
            //         .GetService<MainWindow>();
            //     // exitCode = lifetime.Start(args);
            //     exitCode = (await lifetime.StartAsync(args)).exitCode;
            // }
            return exitCode;
            // builder.Start(AppMain, args);
        }
        
        // public static ThemeSelector Selector;
        
        // private static void AppMain(Application app, string[] args)
        // {
        //     // Selector = ThemeSelector.Create("Themes");
        //     // Selector.LoadSelectedTheme("AvaloniaApp.theme");
        //
        //     app.Run(new MainWindow());
        //
        //     // Selector.SaveSelectedTheme("AvaloniaApp.theme");
        // }
    }
}