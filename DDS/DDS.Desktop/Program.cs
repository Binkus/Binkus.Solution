﻿using System;
using Avalonia;
using Avalonia.ReactiveUI;
using DDS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DDS.Desktop
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                // .UseReactiveUI()
                .ConfigureAppServices(services =>
                {
                    
                })
                .ConfigureAppServicesAfterEverythingElse(services =>
                {
                    services.AddSingleton<IAvaloniaEssentials, AvaloniaEssentialsDesktopService>();
                })
                .UsePlatformDetect()
                .LogToTrace()
        ;
    }
}