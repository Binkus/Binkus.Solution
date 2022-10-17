using Android.App;
using Android.Content.PM;
using Avalonia.Android;
using Avalonia;
using Avalonia.ReactiveUI;

namespace DDS.Android
{
    [Activity(Label = "DDS.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleInstance,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AvaloniaActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            builder.UseReactiveUI();
            return base.CustomizeAppBuilder(builder);
        }
    }
}