using Android.App;
using Android.Content;
using Android.OS;
using Avalonia.Android;
using DDS.Services;
using Microsoft.Extensions.DependencyInjection;
using Application = Android.App.Application;

namespace DDS.Android
{
    [Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : AvaloniaSplashActivity<App>
    {
        protected override Avalonia.AppBuilder CustomizeAppBuilder(Avalonia.AppBuilder builder)
            => Globals.IsStartupDone ? base.CustomizeAppBuilder(builder) : base.CustomizeAppBuilder(builder) 
                .ConfigureAppServices()
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