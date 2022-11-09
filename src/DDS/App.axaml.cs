using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DDS.ViewModels;
using DDS.Views;

namespace DDS
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // MainWindow mainWindow;
                // desktop.MainWindow = mainWindow = Globals.ServiceProvider.GetRequiredService<MainWindow>();
                desktop.MainWindow = Globals.ServiceProvider.GetRequiredService<MainWindow>();
                desktop.MainWindow.Height = 920;
                desktop.MainWindow.Width = 460;
                // mainWindow.SetWindowStartupLocationWorkaround();
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = Globals.ServiceProvider.GetRequiredService<MainView>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}