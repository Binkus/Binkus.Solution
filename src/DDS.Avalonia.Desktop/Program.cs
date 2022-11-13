using Avalonia;
using DDS.Avalonia.Desktop.Services;
using DDS.Avalonia.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DDS.Avalonia.Desktop
{
    internal static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .ConfigureAppServices(services => services
                        .AddSingleton<ICloseAppService,CloseAppService>()
                    )
                .UsePlatformDetect()
                .LogToTrace();
    }
}