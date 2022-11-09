using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

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
                desktop.MainWindow = Globals.ServiceProvider.GetRequiredService<MainWindow>();
                desktop.MainWindow.Height = 920;
                desktop.MainWindow.Width = 460;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = Globals.ServiceProvider.GetRequiredService<MainView>();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}