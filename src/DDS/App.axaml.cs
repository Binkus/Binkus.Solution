using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace DDS;

public sealed partial class App : Application
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
            // desktop.MainWindow = mainWindow = Globals.Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = Globals.Services.GetRequiredService<MainWindow>();
            desktop.MainWindow.Height = 920;
            desktop.MainWindow.Width = 460;
            // mainWindow.SetWindowStartupLocationWorkaround();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = Globals.Services.GetRequiredService<MainView>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}