using System.Reactive;
using System.Windows.Input;
using Android.App;
using Android.Content.PM;
using Avalonia.Android;
using Binkus.ReactiveMvvm;
using DDS.Avalonia.Android.Controls;
using DDS.Avalonia.Services;
using DDS.Core.ViewModels;
using DDS.Core;
using DDS.Core.Controls;
using DDS.Core.Helper;
using DDS.Core.Services;
using DDS.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DDS.Avalonia.Android
{
    [Activity(Label = "DDS.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AvaloniaMainActivity
    {
        private readonly Lazy<INavigationViewModel> _navigation;

        internal static MainActivity? CurrentMainActivity;
        
        public MainActivity()
        {
            CurrentMainActivity = this;
            _navigation = new Lazy<INavigationViewModel>(() => Globals.GetService<ServiceScopeManager>().GetMainScope().GetService<INavigationViewModel>());
        }

        private int _backCounter;
        
        public override void OnBackPressed()
        {
            // base.OnBackPressed(); // => OnResume or OnCreate => InvalidOperationException cause building again

            if (((ICommand)_navigation.Value.BackCommand).CanExecute(null))
            {
                _backCounter = 0;
                _navigation.Value.BackCommand.Execute(Unit.Default).SubscribeAndDisposeOnNext();
                return;
            }
            if(++_backCounter == 2)
            {
                KillApp();
                return;
            }
            ResetCounterAfter(TimeSpan.FromMilliseconds(690));
        }
        
        private void ResetCounterAfter(TimeSpan delay) =>
            Task.Run(async () =>
            {
                await Task.Delay(delay);
                _backCounter = 0;
            });

        private static void KillApp()
        {
            Globals.GetService<IDialogAlertMessageBox>().Show(c =>
            {
                c.Title = "Alert";
                c.Message = "Close App?";
                c.Button2Text = "Cancel";
                c.Button1Action = () => Globals.GetService<ServiceScopeManager>().GetMainScope().GetService<ICloseAppService>().CloseApp();
            });
            
        }
    }
}
