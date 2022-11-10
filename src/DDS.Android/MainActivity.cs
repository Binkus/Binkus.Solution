﻿using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace DDS.Android
{
    [Activity(Label = "DDS.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AvaloniaMainActivity
    {
        public override void OnBackPressed()
        {
            // base.OnBackPressed(); // => OnResume or OnCreate => InvalidOperationException cause building again
        }
    }
}