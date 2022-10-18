using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
                var provider = Globals.ServiceProvider;

                var view = provider.GetRequiredService<MainView>();


                desktop.MainWindow = provider.GetRequiredService<MainWindow>();


                // var view = new MainView { DataContext = new MainViewModel() };
                //
                // desktop.MainWindow = new MainWindow
                // {
                //     DataContext = new MainWindowViewModel { MainView = view }
                // };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}