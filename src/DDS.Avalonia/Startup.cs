using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using Binkus.ReactiveMvvm;
using DDS.Avalonia.Controls.Application;
using DDS.Core;
using DDS.Core.Controls;
using DDS.Core.Helper;
using DDS.Core.Services;
using DDS.Core.Services.Installer;
using DDS.Core.ViewModels;
using DynamicData;
using Microsoft.VisualStudio.Threading;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace DDS.Avalonia;

public static class Startup
{
    public static long StartTimestamp { get; }

    static Startup()
    {
        StartTimestamp = 0.AddTimestamp();
    }

    private static Action<IServiceCollection>? _servicesActionAfterSetup;
    private static IServiceProviderFactory<IServiceCollection>? _serviceProviderFactory;
    public static TAppBuilder ConfigureAppServices<TAppBuilder>(
        this TAppBuilder appBuilder,
        Action<IServiceCollection>? servicesAction = default,
        Action<IServiceCollection>? servicesActionAfterSetup = default,
        IServiceCollection? services = default,
        IServiceProviderFactory<IServiceCollection>? serviceProviderFactory = default
    )
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        _servicesActionAfterSetup = servicesActionAfterSetup;
        _serviceProviderFactory = serviceProviderFactory;
        services ??= new ServiceCollection();
        services = serviceProviderFactory?.CreateBuilder(services) ?? services;
        return appBuilder.ConfigureAppServices(services, servicesAction);
    }

    private static TAppBuilder ConfigureAppServices<TAppBuilder>(
        this TAppBuilder appBuilder,  IServiceCollection services, Action<IServiceCollection>? servicesAction
    )
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        Globals.ISetGlobalsOnlyOnceOnStartup.IsDesignMode = Design.IsDesignMode;
        services.UseMicrosoftDependencyResolver();
        appBuilder.UseReactiveUI(); // Most of ReactiveUI is initialized here already, so DI additions below here:
        StartTimestamp.LogTime<PerformanceLogger.StartupPerformance>().Save();
        var time = 0.AddTimestamp();
        servicesAction?.Invoke(services);
        appBuilder.ConfigureBuilder(services);
        time.LogTime<PerformanceLogger.StartupConfigureBuilderPerformance>().Save();
        return appBuilder;
    }
    
    private static TAppBuilder ConfigureBuilder<TAppBuilder>(this TAppBuilder appBuilder, IServiceCollection services)
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        => appBuilder.AfterSetup(appBuilderAfterSetup =>
        {
            var time = 0.AddTimestamp();
            // callback after Application has been constructed; Application itself may register services
            // which is why we build the ServiceProvider after it has been built,
            // ServiceProvider.UseMicrosoftDependencyResolver (extension method by Splat) throws exception
            // when BuildServiceProvider before App has been built, order matters

            if (!appBuilderAfterSetup.ApplicationType!.IsAssignableTo(typeof(App)))
                throw new InvalidOperationException("App has to implement IAppCore");
            
            Globals.ISetGlobalsOnlyOnceOnStartup.InstanceNullable = appBuilderAfterSetup.Instance 
                as App ?? throw new InvalidOperationException("App has to implement IAppCore or Instance is absent, " +
                                                              "consider checking breaking changes of recent Avalonia updates");
            
            if (!Globals.IsDesignMode) // => appBuilderAfterSetup.Instance.ApplicationLifetime is null when IsDesignMode
            {
                Globals.ISetGlobalsOnlyOnceOnStartup.ApplicationLifetime = appBuilderAfterSetup.Instance?.ApplicationLifetime
                                                          ?? throw new InvalidOperationException(
                                                              "Instance of Application or ApplicationLifetime is null");
                
                Globals.ISetGlobalsOnlyOnceOnStartup.IsClassicDesktopStyleApplicationLifetime =
                    appBuilderAfterSetup.Instance.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;
                
                Globals.ISetGlobalsOnlyOnceOnStartup.ApplicationLifetimeWrapped =
                    Globals.IsClassicDesktopStyleApplicationLifetime
                        ? new DesktopLifetimeWrapper(
                            (IClassicDesktopStyleApplicationLifetime)Globals.ApplicationLifetime) 
                        : new SingleViewLifetimeWrapper((ISingleViewApplicationLifetime)Globals.ApplicationLifetime);
            }
            services.AddSingleton<IAppCore>(Globals.Instance);
            
            Globals.ISetGlobalsOnlyOnceOnStartup.JoinUiTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
            services.AddSingleton<JoinableTaskFactory>(Globals.JoinUiTaskFactory);
            
            _ = services.ConfigureAppServiceProvider(); // => kick-starting all our registrations
            
            Globals.ISetGlobalsOnlyOnceOnStartup.FinishGlobalsSetupByMakingGlobalsImmutable();
            
            var diTime = PerformanceLogger.TryGetResult<PerformanceLogger.DependencyInjectionPerformance>();
            time.GetElapsedTime().Subtract(diTime ?? 0.Ticks()).LogTime<PerformanceLogger.AfterSetupPerformance>().Save();
        });
    
    private static IServiceProvider ConfigureAppServiceProvider(this IServiceCollection services)
    {
        var time = 0.AddTimestamp();
        StartupFacade.ConfigureServices(services); // => kick-starting all our registrations
        _ = services.ConfigureAppServices(); // => kick-starting all our registrations
        _servicesActionAfterSetup?.Invoke(services);
        Globals.ISetGlobalsOnlyOnceOnStartup.ServiceProvider = _serviceProviderFactory?.CreateServiceProvider(services)
#if DEBUG
            ?? services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
#else
            ?? services.BuildServiceProvider();
#endif
        Globals.Services.UseMicrosoftDependencyResolver();
        (services as ServiceCollection)?.MakeReadOnly();
        Globals.ISetGlobalsOnlyOnceOnStartup.ServiceCollection = services;

        if (Globals.IsDesignMode)
        {
            // Skip DB Migration when Design Mode
            Globals.ISetGlobalsOnlyOnceOnStartup.DbMigrationTask = Task.CompletedTask;
            return Globals.Services;
        }
        
        // // Future DB setup
        // SQLitePCL.Batteries_V2.Init();
        Globals.ISetGlobalsOnlyOnceOnStartup.DbMigrationTask = Task.Run(async () =>
        {
            // ExecuteOnServiceProviderCreation.Clear();
            // ExecuteOnServiceProviderCreation = null;
            // using var scope = IAppCore.ServiceProvider.CreateScope();
            // using var context = scope.ServiceProvider.GetRequiredService<AvaloniaDbContext>();
            // await context.Database.MigrateAsync();
            // ReSharper disable once ConvertToLambdaExpression
            await Task.Yield();
        }, CancellationToken.None);
        time.LogTime<PerformanceLogger.DependencyInjectionPerformance>().Save();
        return Globals.Services;
    }
    
    private static IServiceCollection ConfigureAppServices(this IServiceCollection services)
        => services
            .AddSingleton<ServiceScopeManager>()
            .AddLazyResolution()
            .AddScoped<IAvaloniaEssentials,AvaloniaEssentialsCommonService>()
            .AddViewsAndViewModels();
    
    //

    private static IServiceCollection AddViewsAndViewModels(this IServiceCollection services)
    {
        services
            .AddScoped<TopLevelService>()
            .AddSingleton<IViewLocator, ReactiveViewLocator>()
            .AddSingleton<Controls.ViewLocator>()
            .AddSingleton<ApplicationViewModel>();
        
        services.AddViewViewModel<MainView, MainViewModel>(setDataContext: true)
            .AddScoped(typeof(INavigationViewModel<>), typeof(NavigationViewModel<>))
            // .AddTransient<INavigationViewModel, INavigationViewModel<INavigationViewModel>>(p => 
            //     p.GetRequiredService<INavigationViewModel<INavigationViewModel>>())
            .AddScoped<INavigationViewModel, NavigationViewModel>()
            .AddTransient<IScreen, INavigationViewModel>(p => 
                p.GetRequiredService<INavigationViewModel>())
            ;
        StartupFacade.ConfigureViewViewModels(services);
        services.AddWindows();
        StartupFacade.ConfigureWindowViewModels(services);
        return services;
    }

    

    private static IServiceCollection AddWindows(this IServiceCollection services)
        => !Globals.IsClassicDesktopStyleApplicationLifetime 
            ? services : services
                .AddViewViewModel<MainWindow, MainWindowViewModel>(setDataContext: true);
}