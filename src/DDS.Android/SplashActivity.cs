using Android.App;
using Android.Content;
using Avalonia.Android;
using Application = Android.App.Application;
// using DDS.Android.Services;
// using DDS.Mobile.Services;
using DDS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DDS.Android
{
    [Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
    // public class SplashActivity : Activity
    public class SplashActivity : AvaloniaSplashActivity<App>
    {
        protected override Avalonia.AppBuilder CustomizeAppBuilder(Avalonia.AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .ConfigureAppServices(services =>
                {
                    
                })
                .ConfigureAppServicesAfterEverythingElse(services =>
                {
                    // services.AddSingleton<IAvaloniaEssentials, AvaloniaEssentialsMobileService>();
                    services.AddSingleton<IAvaloniaEssentials, AvaloniaEssentialsDesktopService>();
                })
                .AfterSetup(_ =>
                {
                    // Pages.EmbedSample.Implementation = new EmbedSampleAndroid();
                });
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
            }
            catch (InvalidOperationException e)
            {
                // Console.WriteLine(e);
                // throw;
            }
        }
        
        protected override void OnResume()
        {
            base.OnResume();

            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }
        
    }
}