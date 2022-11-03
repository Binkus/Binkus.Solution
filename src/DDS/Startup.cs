using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using DDS.Services;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace DDS;

public static class Startup
{
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
        Globals.ISetGlobalsOnlyOnceOnStartup.IsDesignMode = Design.IsDesignMode;
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
            Globals.ISetGlobalsOnlyOnceOnStartup.InstanceNullable = appBuilderAfterSetup.Instance as App;
            if (Globals.ISetGlobalsOnlyOnceOnStartup.InstanceNullable == null)
                throw new InvalidOperationException("App has to implement IAppCore or Instance is absent, " +
                                    "consider checking breaking changes of recent Avalonia updates");
            if (!Globals.IsDesignMode) // => lifetime null
            {
                Globals.ISetGlobalsOnlyOnceOnStartup.ApplicationLifetime = appBuilderAfterSetup.Instance?.ApplicationLifetime
                                                          ?? throw new InvalidOperationException(
                                                              "Instance of Application or ApplicationLifetime is null"); 
                Globals.ISetGlobalsOnlyOnceOnStartup.IsClassicDesktopStyleApplicationLifetime =
                    appBuilderAfterSetup.Instance.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;
            }
            _ = services.ConfigureAppServiceProvider();
            Globals.ISetGlobalsOnlyOnceOnStartup.FinishGlobalsSetupByMakingGlobalsImmutable();
        });
    
    private static IServiceProvider ConfigureAppServiceProvider(this IServiceCollection services)
    {
        _ = services.ConfigureAppServices(); // => kick-starting all our registrations
        Globals.ISetGlobalsOnlyOnceOnStartup.ServiceProvider = services.BuildServiceProvider();
        Globals.ServiceProvider.UseMicrosoftDependencyResolver();

        if (Globals.IsDesignMode)
        {
            // Skip DB Migration when Design Mode
            Globals.ISetGlobalsOnlyOnceOnStartup.DbMigrationTask = Task.CompletedTask;
            return Globals.ServiceProvider;
        }
        
        // // Future DB setup
        // SQLitePCL.Batteries_V2.Init();
        Globals.ISetGlobalsOnlyOnceOnStartup.DbMigrationTask = Task.Run(() =>
        {
            // ExecuteOnServiceProviderCreation.Clear();
            // ExecuteOnServiceProviderCreation = null;
            // using var scope = IAppCore.ServiceProvider.CreateScope();
            // using var context = scope.ServiceProvider.GetRequiredService<AvaloniaDbContext>();
            // return context.Database.MigrateAsync();
            // ReSharper disable once ConvertToLambdaExpression
            return Task.CompletedTask;
        });
        return Globals.ServiceProvider;
    }
    
    public static IServiceCollection ConfigureAppServices(this IServiceCollection services)
        => services
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


    private static IServiceCollection AddViewAndViewModels(this IServiceCollection services)
        => services
            // .AddSingleton<MainViewModel>()
            // .AddSingleton<MainView>(p => new MainView { DataContext = p.GetRequiredService<MainViewModel>() } )
            .AddSingleton<ReactiveUI.IViewLocator,ReactiveViewLocator>()
            .AddViewAndViewModels<MainView,MainViewModel>(ServiceLifetime.Singleton)
            .AddViewAndViewModels<SecondTestView,SecondTestViewModel>(ServiceLifetime.Singleton)
            .AddViewAndViewModels<TestView,TestViewModel>(ServiceLifetime.Singleton)
            .AddWindows() 
            ;

    private static IServiceProvider ToScopedWhenScoped(this IServiceProvider p, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
        => lifetime == ServiceLifetime.Scoped ? p.CreateScope().ServiceProvider : p;
    
    
    private static IServiceCollection AddViewAndViewModels<TView,TViewModel>(this IServiceCollection services, 
        ServiceLifetime lifetime, Action<IServiceProvider, TView>? postViewCreationAction = default, 
        Action<IServiceProvider, TViewModel>? postViewModelCreationAction = default) 
        where TView : ContentControl, IViewFor<TViewModel>
        where TViewModel : ViewModelBase
    {
        services.Add(ServiceDescriptor.Describe(typeof(TViewModel), p =>
        {
            p = p.ToScopedWhenScoped(lifetime);
            var viewModel = ActivatorUtilities.CreateInstance<TViewModel>(p);
            postViewModelCreationAction?.Invoke(p, viewModel);
            return viewModel;
        }, lifetime is ServiceLifetime.Scoped ? ServiceLifetime.Transient : lifetime));
        services.Add(ServiceDescriptor.Describe(typeof(TView), p =>
        {
            p = p.ToScopedWhenScoped(lifetime);
            var view = ActivatorUtilities.CreateInstance<TView>(p);
            view.DataContext = p.GetRequiredService<TViewModel>();
            postViewCreationAction?.Invoke(p, view);
            return view;
            // return new TView { DataContext = p.GetRequiredService<TViewModel>() };
        // }, lifetime is ServiceLifetime.Scoped ? ServiceLifetime.Transient : lifetime));
        }, ServiceLifetime.Transient));
        ReactiveViewLocator.DictOfViews[typeof(TViewModel)] = typeof(TView);
        return services;
    }

    private static IServiceCollection AddWindows(this IServiceCollection service)
        => !Globals.IsClassicDesktopStyleApplicationLifetime 
            ? service.AddSingleton<TopLevel>(p => (TopLevel)p.GetRequiredService<MainView>().GetVisualRoot()!)
            : service.AddSingleton<TopLevel>(p => p.GetRequiredService<MainWindow>())
            // .AddSingleton<MainWindowViewModel>(p => new MainWindowViewModel { MainView = p.GetRequiredService<MainView>() })
            // .AddSingleton<MainWindow>(p => new MainWindow { DataContext = p.GetRequiredService<MainWindowViewModel>() } )
            // .AddViewAndViewModels<MainWindow, MainWindowViewModel>(ServiceLifetime.Singleton, 
            //     postViewModelCreationAction: (p, vm) 
            //         => vm.MainView = p.GetRequiredService<MainView>())
            .AddViewAndViewModels<MainWindow, MainWindowViewModel>(ServiceLifetime.Singleton)
            ;
}