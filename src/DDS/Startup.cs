using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using DDS.Services;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace DDS;

public static class Startup
{
    // private static Action<IServiceCollection>? ConfigureAppServicesAfterEverythingElseAction { get; set; }
    
    // public static TAppBuilder ConfigureAppServicesAfterEverythingElse<TAppBuilder>(
    //     this TAppBuilder appBuilder, Action<IServiceCollection>? serviceCollectionAction = default
    // )
    //     where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    // {
    //     ConfigureAppServicesAfterEverythingElseAction = serviceCollectionAction;
    //     return appBuilder;
    // }
    
    public static TAppBuilder ConfigureAppServices<TAppBuilder>(
        this TAppBuilder appBuilder, Action<IServiceCollection>? serviceCollectionAction = default
    )
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        return appBuilder.ConfigureAppServices(new ServiceCollection(), serviceCollectionAction);
    }
    
    
    private static TAppBuilder ConfigureAppServices<TAppBuilder>(
        this TAppBuilder appBuilder,  IServiceCollection services, Action<IServiceCollection>? serviceCollectionAction
    )
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        Globals.ISetGlobals.IsDesignMode = Design.IsDesignMode;
        services.UseMicrosoftDependencyResolver();
        var resolver = Locator.CurrentMutable;
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();
        appBuilder.UseReactiveUI();
        serviceCollectionAction?.Invoke(services);
        return appBuilder.ConfigureBuilder(services);
    }
    
    private static TAppBuilder ConfigureBuilder<TAppBuilder>(this TAppBuilder appBuilder, IServiceCollection services)
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        => appBuilder.AfterSetup(appBuilderAfterSetup =>
        { 
            // callback after Application has been constructed; Application itself may register services
            // which is why we build the ServiceProvider after it has been built,
            // ServiceProvider.UseMicrosoftDependencyResolver (extension method by Splat) throws exception
            // when BuildServiceProvider before App has been built, order matters

            if (!appBuilderAfterSetup.ApplicationType!.IsAssignableTo(typeof(App)))
                throw new InvalidOperationException("App has to implement IAppCore");
            Globals.ISetGlobals.InstanceNullable = appBuilderAfterSetup.Instance as App;
            if (Globals.ISetGlobals.InstanceNullable == null)
                throw new InvalidOperationException("App has to implement IAppCore or Instance is absent, " +
                                    "consider checking breaking changes of recent Avalonia updates");
            if (!Globals.IsDesignMode) // => lifetime null
            {
                Globals.ISetGlobals.ApplicationLifetime = appBuilderAfterSetup.Instance?.ApplicationLifetime
                                                          ?? throw new InvalidOperationException(
                                                              "Instance of Application or ApplicationLifetime is null"); 
                Globals.ISetGlobals.IsClassicDesktopStyleApplicationLifetime =
                    appBuilderAfterSetup.Instance.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;
            }
            _ = services.ConfigureAppServiceProvider();
        });
    
    private static IServiceProvider ConfigureAppServiceProvider(this IServiceCollection services)
    {
        _ = services.ConfigureAppServices(); // => kick-starting all our registrations
        // ConfigureAppServicesAfterEverythingElseAction?.Invoke(services);
        Globals.ISetGlobals.ServiceProvider = services.BuildServiceProvider();
        Globals.ServiceProvider.UseMicrosoftDependencyResolver();
        // ExecuteOnServiceProviderCreation!.ForEach(action => action.Invoke(Globals.ServiceProvider));
        if (Globals.IsDesignMode) return Globals.ServiceProvider;
        // SQLitePCL.Batteries_V2.Init();
        // Globals.DbMigrationTask = Task.Run(() =>
        // {
        //     ExecuteOnServiceProviderCreation.Clear();
        //     ExecuteOnServiceProviderCreation = null;
        //     using var scope = IAppCore.ServiceProvider.CreateScope(); // IAppCore.Instance.Services.CreateScope();
        //     using var context = scope.ServiceProvider.GetRequiredService<AvaloniaDbContext>();
        //     return context.Database.MigrateAsync();
        // });
        return Globals.ServiceProvider;
    }
    
    public static IServiceCollection ConfigureAppServices(this IServiceCollection services)
        => Globals.IsDesignMode ? services : services
            .AddLazyResolution()
            .AddSingleton<IAvaloniaEssentials,AvaloniaEssentialsCommonService>()
            .AddViewAndViewModels();
    
    private static IServiceCollection AddLazyResolution(this IServiceCollection services) 
        => services.AddTransient(
            typeof(Lazy<>),
            typeof(LazilyResolved<>));

    private class LazilyResolved<T> : Lazy<T> where T : notnull
    {
        public LazilyResolved(IServiceProvider serviceProvider)
            : base(serviceProvider.GetRequiredService<T>)
        {
        }
    }


    private static IServiceCollection AddViewAndViewModels(this IServiceCollection service)
        => service
            .AddSingleton<MainViewModel>()
            // .AddSingleton<MainWindowViewModel,MainWindowViewModel>(p => ActivatorUtilities.CreateInstance<MainWindowViewModel>(p))
            .AddSingleton<MainView>(p => new MainView { DataContext = p.GetRequiredService<MainViewModel>() } )
            
            .AddWindows() 
            
            // .AddSingleton<MainWindowViewModel>(p => new MainWindowViewModel { MainView = p.GetRequiredService<MainView>() } )
            
            // .AddSingleton<MainWindow>(p => new MainWindow { DataContext = p.GetRequiredService<MainWindowViewModel>() } )
            // .AddSingleton<>()
            ;

    private static IServiceCollection AddWindows(this IServiceCollection service)
        => !Globals.IsClassicDesktopStyleApplicationLifetime 
            ? service.AddSingleton<TopLevel>(p => (TopLevel)p.GetRequiredService<MainView>().GetVisualRoot()!)
            : service.AddSingleton<TopLevel>(p => p.GetRequiredService<MainWindow>())
            .AddSingleton<MainWindowViewModel>(p => new MainWindowViewModel { MainView = p.GetRequiredService<MainView>() })
            .AddSingleton<MainWindow>(p => new MainWindow { DataContext = p.GetRequiredService<MainWindowViewModel>() } )
            ;
}