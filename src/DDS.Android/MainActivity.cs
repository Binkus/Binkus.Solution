using System.Reactive;
using System.Windows.Input;
using Android.App;
using Android.Content.PM;
using Avalonia.Android;
using DDS.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DDS.Android
{
    [Activity(Label = "DDS.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AvaloniaMainActivity
    {
        private readonly Lazy<NavigationViewModel> _navigation;
        
        public MainActivity()
        {
            _navigation = Globals.ServiceProvider.GetRequiredService<Lazy<NavigationViewModel>>();
        }
        
        public override void OnBackPressed()
        {
            // base.OnBackPressed(); // => OnResume or OnCreate => InvalidOperationException cause building again

            if (((ICommand)_navigation.Value.GoBack).CanExecute(null))
                _navigation.Value.GoBack.Execute(Unit.Default).Subscribe();
        }
    }
}