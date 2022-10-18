using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Avalonia;
using Avalonia.ReactiveUI;
using DDS.Android.Services;
using DDS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DDS.Android
{
    [Activity(Label = "DDS.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleInstance,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AvaloniaActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            builder
                // .UseReactiveUI()
                .ConfigureAppServices(services =>
                {
                    services.AddSingleton<IAvaloniaEssentials, AvaloniaEssentialsMobileService>();
                });
            return base.CustomizeAppBuilder(builder);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            //...
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this,
                savedInstanceState); // add this line to your code, it may also be called: bundle
            //...
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}