using Android.App;
using Android.Content;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using DDS.Avalonia.Android.Services;
using DDS.Avalonia.Services;
using DDS.Core;
using DDS.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Application = Android.App.Application;

namespace DDS.Avalonia.Android
{
    [Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : AvaloniaSplashActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
            => Globals.IsStartupDone ? base.CustomizeAppBuilder(builder) : base.CustomizeAppBuilder(builder) 
                .ConfigureAppServices(services => services.AddSingleton<ICloseAppService,CloseAppService>())
                // .ConfigureAppServicesAfterEverythingElse(services =>
                //     services.AddSingleton<IAvaloniaEssentials, AvaloniaEssentialsDesktopService>()
                // )
            ;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected override void OnResume()
        {
            base.OnResume();
            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }
    }
}