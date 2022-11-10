using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
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
        appBuilder.UseReactiveUI(); // Most of ReactiveUI is initialized here already, so DI additions below here: 
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
            if (!Globals.IsDesignMode) // => appBuilderAfterSetup.Instance.ApplicationLifetime is null when IsDesignMode
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
        StartupFacade.ConfigureServices(services);
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
    {
        services
            .AddSingleton<IViewLocator, ReactiveViewLocator>()
            .AddViewAndViewModels<MainView, MainViewModel>(ServiceLifetime.Singleton, setDataContext: true)
            .AddSingleton<NavigationViewModel>()
            .AddSingleton<IScreen, NavigationViewModel>(p => p.GetRequiredService<NavigationViewModel>())
            // .AddViewAndViewModels<SecondTestView,SecondTestViewModel>(ServiceLifetime.Singleton)
            // .AddViewAndViewModels<TestView,TestViewModel>(ServiceLifetime.Singleton)
            ;
        StartupFacade.ConfigureViewViewModels(services);
        services.AddWindows();
        StartupFacade.ConfigureWindowViewModels(services);
        return services;
    }

    private static IServiceProvider ToScopedWhenScoped(this IServiceProvider p, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
        => lifetime == ServiceLifetime.Scoped ? p.CreateScope().ServiceProvider : p;
    
    
    public static IServiceCollection AddViewAndViewModels<TView,TViewModel>(this IServiceCollection services,
        ServiceLifetime lifetime,
        Func<IServiceProvider, TView>? viewImplFactory = default,
        Func<IServiceProvider, TViewModel>? viewModelImplFactory = default,
        Action<IServiceProvider, TView>? postViewCreationAction = default, 
        Action<IServiceProvider, TViewModel>? postViewModelCreationAction = default,
        bool setDataContext = false)
        where TView : ContentControl, IViewFor<TViewModel>
        where TViewModel : class
    {
        // Register ViewModel
        services.Add(ServiceDescriptor.Describe(typeof(TViewModel), p =>
        {
            p = p.ToScopedWhenScoped(lifetime);
            var viewModel = viewModelImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance<TViewModel>(p);
            postViewModelCreationAction?.Invoke(p, viewModel);
            return viewModel;
        }, lifetime is ServiceLifetime.Scoped ? ServiceLifetime.Transient : lifetime));

        // Register View
        // Views should be Transient when not [SingleInstanceView], but Singleton or scoped can be buggy except for MainView.
        var viewLifetime = ServiceLifetime.Transient;
        
        services.Add(ServiceDescriptor.Describe(typeof(TView), p =>
        {
            var view = viewImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance<TView>(p);
            if (setDataContext) view.DataContext = p.GetRequiredService<TViewModel>();
            postViewCreationAction?.Invoke(p, view);
            return view;
        }, viewLifetime));
        
        services.Add(ServiceDescriptor.Describe(typeof(IViewFor<TViewModel>), 
            p => p.GetRequiredService<TView>(), viewLifetime));
        
        if (typeof(TView).IsAssignableTo(typeof(BaseUserControl<TViewModel>)))
        {
            services.Add(ServiceDescriptor.Describe(typeof(BaseUserControl<TViewModel>), 
                p => p.GetRequiredService<TView>(), viewLifetime));
        }
        else if (typeof(TView).IsAssignableTo(typeof(BaseWindow<TViewModel>)))
        {
            services.Add(ServiceDescriptor.Describe(typeof(BaseWindow<TViewModel>), 
                p => p.GetRequiredService<TView>(), viewLifetime));
        }
        ReactiveViewLocator.DictOfViews[typeof(TViewModel).FullName ?? throw new NullReferenceException()] = typeof(TView);
        return services;
    }

    private static IServiceCollection AddWindows(this IServiceCollection service)
        => !Globals.IsClassicDesktopStyleApplicationLifetime 
            ? service.AddSingleton<TopLevel>(p => (TopLevel)p.GetRequiredService<MainView>().GetVisualRoot()!)
            : service.AddSingleton<TopLevel>(p => p.GetRequiredService<MainWindow>())
            .AddViewAndViewModels<MainWindow, MainWindowViewModel>(ServiceLifetime.Singleton, setDataContext: true)
            ;
}